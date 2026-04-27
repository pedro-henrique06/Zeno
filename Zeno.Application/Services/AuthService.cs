using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using FluentValidation;
using FluentValidation.Results;
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
        await ValidateAsync<LoginRequestValidator, LoginRequest>(request);

        var user = await _userRepository.GetByEmailAsync(request.Email)
            ?? throw new AppValidationException(new ValidationResult(
                new List<FvValidationFailure>
                {
                    new("Email", "Usuário não encontrado.")
                }));

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new AppValidationException(new ValidationResult(
                new List<FvValidationFailure>
                {
                    new("Password", "Senha inválida.")
                }));

        var token = GenerateJwtToken(user);

        return new AuthResponse
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Token = token
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        await ValidateAsync<RegisterRequestValidator, RegisterRequest>(request);

        if (await _userRepository.EmailExistsAsync(request.Email))
            throw new AppValidationException(new ValidationResult(
                new List<FvValidationFailure>
                {
                    new("Email", "Este e-mail já está em uso.")
                }));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);

        var token = GenerateJwtToken(user);

        return new AuthResponse
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
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

    private async Task ValidateAsync<TValidator, T>(T instance) where TValidator : IValidator<T>
    {
        var validator = (TValidator)_serviceProvider.GetService(typeof(TValidator))!;
        var result = await validator.ValidateAsync(instance!);

        if (!result.IsValid)
            throw new AppValidationException(result);
    }
}
