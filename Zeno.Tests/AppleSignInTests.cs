using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Services;
using Zeno.Domain.Auth;
using Zeno.Domain.Interfaces;
using Zeno.Domain.User;

namespace Zeno.Tests;

/// <summary>Stands in for appleid.apple.com/auth/keys with a key generated per test run.</summary>
internal sealed class FakeAppleKeys : HttpMessageHandler
{
    public RSA Rsa { get; } = RSA.Create(2048);
    public string KeyId { get; } = "test-kid";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var p = Rsa.ExportParameters(false);
        var jwks = new
        {
            keys = new[]
            {
                new { kty = "RSA", kid = KeyId, use = "sig", alg = "RS256", n = Base64UrlEncoder.Encode(p.Modulus), e = Base64UrlEncoder.Encode(p.Exponent) }
            }
        };
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(jwks)) });
    }

    public string Token(
        string sub = "apple-sub-1",
        string? email = "ana@privaterelay.appleid.com",
        string audience = "app.zeno.mobile",
        string issuer = "https://appleid.apple.com",
        DateTime? expires = null,
        RSA? signWith = null)
    {
        var claims = new List<Claim> { new("sub", sub) };
        if (email is not null)
        {
            claims.Add(new Claim("email", email));
            claims.Add(new Claim("email_verified", "true"));
        }
        var key = new RsaSecurityKey(signWith ?? Rsa) { KeyId = KeyId };
        var exp = expires ?? DateTime.UtcNow.AddMinutes(10);
        var jwt = new JwtSecurityToken(issuer, audience, claims, exp.AddMinutes(-20), exp,
            new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}

public class AppleIdentityTokenValidatorTests
{
    private readonly FakeAppleKeys _apple = new();

    private AppleIdentityTokenValidator CreateValidator() =>
        new(new HttpClient(_apple), new MemoryCache(new MemoryCacheOptions()), new Mock<IConfiguration>().Object);

    [Fact]
    public async Task ValidToken_ReturnsSubjectAndEmail()
    {
        var identity = await CreateValidator().ValidateAsync(_apple.Token());

        Assert.Equal("apple-sub-1", identity.Subject);
        Assert.Equal("ana@privaterelay.appleid.com", identity.Email);
        Assert.True(identity.EmailVerified);
    }

    [Fact]
    public async Task TokenSignedByAnotherKey_IsRejected()
    {
        using var attacker = RSA.Create(2048);
        await Assert.ThrowsAsync<AppValidationException>(() => CreateValidator().ValidateAsync(_apple.Token(signWith: attacker)));
    }

    [Fact]
    public async Task TokenForAnotherApp_IsRejected()
    {
        await Assert.ThrowsAsync<AppValidationException>(() => CreateValidator().ValidateAsync(_apple.Token(audience: "host.exp.Exponent")));
    }

    [Fact]
    public async Task TokenFromAnotherIssuer_IsRejected()
    {
        await Assert.ThrowsAsync<AppValidationException>(() => CreateValidator().ValidateAsync(_apple.Token(issuer: "https://evil.example")));
    }

    [Fact]
    public async Task ExpiredToken_IsRejected()
    {
        await Assert.ThrowsAsync<AppValidationException>(() => CreateValidator().ValidateAsync(_apple.Token(expires: DateTime.UtcNow.AddMinutes(-5))));
    }

    [Fact]
    public async Task Garbage_IsRejected()
    {
        await Assert.ThrowsAsync<AppValidationException>(() => CreateValidator().ValidateAsync("not-a-jwt"));
    }
}

public class AppleLoginTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRefreshTokenRepository> _tokenRepo = new();
    private readonly Mock<IAppleIdentityTokenValidator> _apple = new();

    public AppleLoginTests()
    {
        _tokenRepo.Setup(r => r.CreateAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken t) => t);
        _userRepo.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _apple.Setup(v => v.ValidateAsync("tok")).ReturnsAsync(new AppleIdentity("sub-1", "ana@icloud.com", true));
    }

    private AuthService CreateService()
    {
        var jwt = new Mock<IConfigurationSection>();
        jwt.SetupGet(s => s["Key"]).Returns("a-jwt-signing-key-with-at-least-32-chars!!");
        jwt.SetupGet(s => s["Issuer"]).Returns("zeno");
        jwt.SetupGet(s => s["Audience"]).Returns("zeno");
        jwt.SetupGet(s => s["ExpiresInHours"]).Returns("1");
        var config = new Mock<IConfiguration>();
        config.Setup(c => c.GetSection("Jwt")).Returns(jwt.Object);

        return new AuthService(
            new Mock<IValidator<LoginRequest>>().Object,
            new Mock<IValidator<RegisterRequest>>().Object,
            _userRepo.Object,
            _tokenRepo.Object,
            config.Object,
            new Mock<ITokenBlacklistService>().Object,
            _apple.Object);
    }

    [Fact]
    public async Task ReturningAppleUser_SignsIn_WithoutCreatingAnother()
    {
        var existing = new User { Id = Guid.NewGuid(), Name = "Ana", Email = "ana@icloud.com", Provider = OAuthProvider.Apple, ProviderId = "sub-1" };
        _userRepo.Setup(r => r.GetByProviderAsync("Apple", "sub-1")).ReturnsAsync(existing);

        var result = await CreateService().LoginWithAppleAsync(new AppleLoginRequest { IdentityToken = "tok" });

        Assert.Equal(existing.Id, result.UserId);
        Assert.False(string.IsNullOrEmpty(result.Token));
        _userRepo.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task NewUser_IsCreated_WithTheNameFromTheApp()
    {
        var result = await CreateService().LoginWithAppleAsync(new AppleLoginRequest { IdentityToken = "tok", FullName = "Ana Souza" });

        Assert.Equal("Ana Souza", result.Name);
        _userRepo.Verify(r => r.CreateAsync(It.Is<User>(u =>
            u.Provider == OAuthProvider.Apple && u.ProviderId == "sub-1" && u.Email == "ana@icloud.com" && u.EmailVerified)), Times.Once);
    }

    [Fact]
    public async Task EmailWithPasswordAccount_IsNotTakenOver()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("ana@icloud.com"))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), Email = "ana@icloud.com", PasswordHash = "hash", Provider = OAuthProvider.None });

        await Assert.ThrowsAsync<AppValidationException>(() => CreateService().LoginWithAppleAsync(new AppleLoginRequest { IdentityToken = "tok" }));
        _userRepo.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
    }
}
