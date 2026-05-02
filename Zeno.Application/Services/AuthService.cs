using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Responses;
using Zeno.Application.Validators;
using Zeno.Domain.Interfaces;
using Zeno.Domain.User;
using FvValidationFailure = FluentValidation.Results.ValidationFailure;

namespace Zeno.Application.Services;

public class AuthService : IAuthService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ITokenBlacklistService _tokenBlacklistService;

    public AuthService(IServiceProvider serviceProvider, IUserRepository userRepository, IConfiguration configuration, ITokenBlacklistService tokenBlacklistService)
    {
        _serviceProvider = serviceProvider;
        _userRepository = userRepository;
        _configuration = configuration;
        _tokenBlacklistService = tokenBlacklistService;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        Console.WriteLine($"[LoginAsync] Starting - Email: {request.Email}");

        try
        {
            await ValidateAsync<LoginRequestValidator, LoginRequest>(request);
            Console.WriteLine("[LoginAsync] Validation passed");

            var user = await _userRepository.GetByEmailAsync(request.Email);
            Console.WriteLine($"[LoginAsync] User found: {user != null}");

            if (user is null)
                throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                    new List<FvValidationFailure>
                    {
                        new("Email", "Usuário não encontrado.")
                    }));

            Console.WriteLine($"[LoginAsync] Provider: {user.Provider}, PasswordHash null: {string.IsNullOrEmpty(user.PasswordHash)}");

            if (user.Provider != OAuthProvider.None && !string.IsNullOrEmpty(user.ProviderId))
                throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                    new List<FvValidationFailure>
                    {
                        new("Email", "Este e-mail está cadastrado via OAuth. Faça login com o provedor correspondente.")
                    }));

            if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                    new List<FvValidationFailure>
                    {
                        new("Password", "Senha inválida.")
                    }));

            var token = GenerateJwtToken(user);
            Console.WriteLine("[LoginAsync] Success!");

            return new AuthResponse
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Document = user.Document,
                BirthDate = user.BirthDate,
                OAuthProvider = user.Provider.ToString(),
                Token = token
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoginAsync] EXCEPTION: {ex.GetType().FullName}: {ex.Message}");
            Console.WriteLine($"[LoginAsync] Stack: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        await ValidateAsync<RegisterRequestValidator, RegisterRequest>(request);

        if (await _userRepository.EmailExistsAsync(request.Email))
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FvValidationFailure>
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
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);

        var token = GenerateJwtToken(user);

        return new AuthResponse
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Document = user.Document,
            BirthDate = user.BirthDate,
            OAuthProvider = user.Provider.ToString(),
            Token = token
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
                new List<FvValidationFailure>
                {
                    new("Email", "Este e-mail já possui cadastro com senha. Faça login com e-mail e senha.")
                }));
        }

        var token = GenerateJwtToken(user);

        return new AuthResponse
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Document = user.Document,
            BirthDate = user.BirthDate,
            OAuthProvider = user.Provider.ToString(),
            Token = token
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

    public string GetGoogleClientId()
    {
        return _configuration["OAuth:Google:ClientId"] ?? "";
    }

    public string GetGoogleClientSecret()
    {
        return _configuration["OAuth:Google:ClientSecret"] ?? "";
    }

    private async Task ValidateAsync<TValidator, T>(T instance) where TValidator : IValidator<T>
    {
        var validator = (TValidator)_serviceProvider.GetService(typeof(TValidator))!;
        var result = await validator.ValidateAsync(instance!);

        if (!result.IsValid)
            throw new AppValidationException(result);
    }
}