using FluentValidation;
using Microsoft.Extensions.Configuration;
using Moq;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Services;
using Zeno.Domain.Auth;
using Zeno.Domain.Interfaces;
using Zeno.Domain.User;

namespace Zeno.Tests;

public class RefreshTokenHashingTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRefreshTokenRepository> _tokenRepo = new();
    private readonly Guid _userId = Guid.NewGuid();

    public RefreshTokenHashingTests()
    {
        _userRepo.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(new User
        {
            Id = _userId,
            Email = "a@b.com",
            Name = "Ana"
        });
        _tokenRepo.Setup(r => r.CreateAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken t) => t);
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
            new Mock<IAppleIdentityTokenValidator>().Object);
    }

    private RefreshToken Active(string storedToken) => new()
    {
        Id = Guid.NewGuid(),
        UserId = _userId,
        Token = storedToken,
        ExpiresAt = DateTime.UtcNow.AddDays(1)
    };

    [Fact]
    public async Task Refresh_FindsTheTokenByItsHash_AndRevokesTheStoredValue()
    {
        var hash = TokenHasher.Hash("raw-token");
        _tokenRepo.Setup(r => r.GetByTokenAsync(hash)).ReturnsAsync(Active(hash));

        var result = await CreateService().RefreshTokenAsync("raw-token");

        _tokenRepo.Verify(r => r.RevokeAsync(_userId, hash), Times.Once);
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
    }

    [Fact]
    public async Task Refresh_NewToken_IsStoredAsHash_AndTheClientGetsThePlainValue()
    {
        var hash = TokenHasher.Hash("raw-token");
        _tokenRepo.Setup(r => r.GetByTokenAsync(hash)).ReturnsAsync(Active(hash));
        RefreshToken? stored = null;
        _tokenRepo.Setup(r => r.CreateAsync(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(t => stored = t)
            .ReturnsAsync((RefreshToken t) => t);

        var result = await CreateService().RefreshTokenAsync("raw-token");

        Assert.NotNull(stored);
        Assert.True(TokenHasher.IsHash(stored!.Token));
        Assert.Equal(TokenHasher.Hash(result.RefreshToken!), stored.Token);
        Assert.NotEqual(result.RefreshToken, stored.Token);
    }

    [Fact]
    public async Task Refresh_AcceptsAPlainTokenIssuedBeforeHashing()
    {
        _tokenRepo.Setup(r => r.GetByTokenAsync(TokenHasher.Hash("legacy-plain-token"))).ReturnsAsync((RefreshToken?)null);
        _tokenRepo.Setup(r => r.GetByTokenAsync("legacy-plain-token")).ReturnsAsync(Active("legacy-plain-token"));

        var result = await CreateService().RefreshTokenAsync("legacy-plain-token");

        _tokenRepo.Verify(r => r.RevokeAsync(_userId, "legacy-plain-token"), Times.Once);
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
    }

    [Fact]
    public async Task Refresh_RejectsAStoredHashPresentedAsIfItWereTheToken()
    {
        // Someone who read the database must not be able to use the hashes they found.
        var stolenHash = TokenHasher.Hash("victim-token");
        _tokenRepo.Setup(r => r.GetByTokenAsync(TokenHasher.Hash(stolenHash))).ReturnsAsync((RefreshToken?)null);
        _tokenRepo.Setup(r => r.GetByTokenAsync(stolenHash)).ReturnsAsync(Active(stolenHash));

        await Assert.ThrowsAsync<AppValidationException>(() => CreateService().RefreshTokenAsync(stolenHash));
        _tokenRepo.Verify(r => r.RevokeAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_UnknownToken_IsRejected()
    {
        _tokenRepo.Setup(r => r.GetByTokenAsync(It.IsAny<string>())).ReturnsAsync((RefreshToken?)null);

        await Assert.ThrowsAsync<AppValidationException>(() => CreateService().RefreshTokenAsync("nope"));
    }

    [Fact]
    public async Task Refresh_ExpiredOrRevokedToken_IsRejected()
    {
        var hash = TokenHasher.Hash("old");
        var expired = Active(hash);
        expired.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        _tokenRepo.Setup(r => r.GetByTokenAsync(hash)).ReturnsAsync(expired);

        await Assert.ThrowsAsync<AppValidationException>(() => CreateService().RefreshTokenAsync("old"));
    }
}
