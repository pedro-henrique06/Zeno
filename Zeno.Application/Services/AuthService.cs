using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Microsoft.IdentityModel.Tokens;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Responses;
using Zeno.Application.Validators;
using Zeno.Domain.Interfaces;
using Zeno.Domain.User;

namespace Zeno.Application.Services;

public class AuthService : IAuthService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserRepository _userRepository;
    private readonly string _jwtKey;
    private readonly string _jwtIssuer;
    private readonly int _jwtExpireHours;

    public AuthService(IServiceProvider serviceProvider, IUserRepository userRepository, string jwtKey, string jwtIssuer, int jwtExpireHours)
    {
        _serviceProvider = serviceProvider;
        _userRepository = userRepository;
        _jwtKey = jwtKey;
        _jwtIssuer = jwtIssuer;
        _jwtExpireHours = jwtExpireHours;
    }

    public async Task<AuthResponse> Register(RegisterRequest request)
    {
        await ValidateAsync<RegisterRequestValidator, RegisterRequest>(request);

        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(request.Email), "Este e-mail já está cadastrado.")
                }));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password)
        };

        await _userRepository.CreateAsync(user);

        return GenerateToken(user);
    }

    public async Task<AuthResponse> Login(LoginRequest request)
    {
        await ValidateAsync<LoginRequestValidator, LoginRequest>(request);

        var user = await _userRepository.GetByEmailAsync(request.Email)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(request.Email), "E-mail ou senha inválidos.")
                }));

        if (!VerifyPassword(request.Password, user.PasswordHash))
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(request.Password), "E-mail ou senha inválidos.")
                }));

        return GenerateToken(user);
    }

    private AuthResponse GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddHours(_jwtExpireHours);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtIssuer,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new AuthResponse
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = refreshToken,
            TokenExpiresAt = expiresAt
        };
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    private async Task ValidateAsync<TValidator, T>(T instance) where TValidator : IValidator<T>
    {
        var validator = (TValidator)_serviceProvider.GetService(typeof(TValidator))!;
        var result = await validator.ValidateAsync(instance!);

        if (!result.IsValid)
            throw new AppValidationException(result);
    }
}
