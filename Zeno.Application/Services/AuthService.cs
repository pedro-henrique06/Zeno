using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Responses;
using Zeno.Domain.Auth;
using Zeno.Domain.Interfaces;
using Zeno.Domain.User;

namespace Zeno.Application.Services;

public class AuthService : IAuthService
{
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IConfiguration _configuration;
    private readonly ITokenBlacklistService _tokenBlacklistService;

    public AuthService(
        IValidator<LoginRequest> loginValidator,
        IValidator<RegisterRequest> registerValidator,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IConfiguration configuration,
        ITokenBlacklistService tokenBlacklistService)
    {
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _configuration = configuration;
        _tokenBlacklistService = tokenBlacklistService;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var user = await _userRepository.GetByEmailAsync(request.Email)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("Email", "Usuário não encontrado.")
                }));

        if (user.Provider != OAuthProvider.None && !string.IsNullOrEmpty(user.ProviderId))
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("Email", "Este e-mail está cadastrado via OAuth. Faça login com o provedor correspondente.")
                }));

        if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("Password", "Senha inválida.")
                }));

        var (token, refreshToken) = await GenerateTokensAsync(user);

        return new AuthResponse
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Document = user.Document,
            BirthDate = user.BirthDate,
            OAuthProvider = user.Provider.ToString(),
            Token = token,
            RefreshToken = refreshToken
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var validation = await _registerValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        if (await _userRepository.EmailExistsAsync(request.Email))
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("Email", "Este e-mail já está em uso.")
                }));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Document = request.Document,
            BirthDate = request.BirthDate,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Provider = OAuthProvider.None,
            Currency = request.Currency ?? Zeno.Domain.Enum.Currency.BRL,
            Language = request.Language ?? Zeno.Domain.Enum.Language.PtBR,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);

        var (token, refreshToken) = await GenerateTokensAsync(user);

        return new AuthResponse
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Document = user.Document,
            BirthDate = user.BirthDate,
            OAuthProvider = user.Provider.ToString(),
            Token = token,
            RefreshToken = refreshToken
        };
    }

    public async Task<AuthResponse> HandleOAuthCallbackAsync(string provider, string providerId, string email, string name)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user is null)
        {
            var oauthProvider = provider.ToLower() switch
            {
                "google" => OAuthProvider.Google,
                _ => OAuthProvider.None
            };

            user = new User
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = email,
                Provider = oauthProvider,
                ProviderId = providerId,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(user);
        }
        else if (user.Provider == OAuthProvider.None && string.IsNullOrEmpty(user.ProviderId) && !string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("Email", "Este e-mail já possui cadastro com senha. Faça login com e-mail e senha.")
                }));
        }

        var (token, refreshToken) = await GenerateTokensAsync(user);

        return new AuthResponse
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Document = user.Document,
            BirthDate = user.BirthDate,
            OAuthProvider = user.Provider.ToString(),
            Token = token,
            RefreshToken = refreshToken
        };
    }

    public async Task LogoutAsync(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value
            ?? throw new InvalidOperationException("Token sem JTI.");

        var expiresIn = jwt.ValidTo - DateTime.UtcNow;
        if (expiresIn > TimeSpan.Zero)
            _tokenBlacklistService.Revoke(jti, expiresIn);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (storedToken is null || !storedToken.IsActive)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("RefreshToken", "Refresh token inválido ou expirado.")
                }));

        var user = await _userRepository.GetByIdAsync(storedToken.UserId);
        if (user is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("RefreshToken", "Usuário não encontrado.")
                }));

        await _refreshTokenRepository.RevokeAsync(user.Id, refreshToken);

        var (token, newRefreshToken) = await GenerateTokensAsync(user);

        return new AuthResponse
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Document = user.Document,
            BirthDate = user.BirthDate,
            OAuthProvider = user.Provider.ToString(),
            Token = token,
            RefreshToken = newRefreshToken
        };
    }

    private async Task<(string Token, string RefreshToken)> GenerateTokensAsync(User user)
    {
        var token = GenerateJwtToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);
        return (token, refreshToken);
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim("Provider", user.Provider.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(double.Parse(jwtSettings["ExpiresInHours"]!)),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> CreateRefreshTokenAsync(Guid userId)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var refreshToken = Convert.ToBase64String(randomBytes);

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.CreateAsync(entity);
        return refreshToken;
    }

    public string GetGoogleClientId() => _configuration["OAuth:Google:ClientId"] ?? "";
    public string GetGoogleClientSecret() => _configuration["OAuth:Google:ClientSecret"] ?? "";
}