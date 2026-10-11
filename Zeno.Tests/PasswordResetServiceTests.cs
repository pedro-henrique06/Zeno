using System.Text.RegularExpressions;
using Moq;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Services;
using Zeno.Application.Validators;
using Zeno.Domain.Auth;
using Zeno.Domain.Interfaces;
using Zeno.Domain.User;

namespace Zeno.Tests;

public class PasswordResetServiceTests
{
    private const string Email = "ana@zeno.test";
    private const string NewPassword = "Nova#Senha1";

    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordResetCodeRepository> _codeRepo = new();
    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly Mock<IEmailSender> _email = new();
    private readonly MutableClock _clock = new() { UtcNow = new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc) };
    private readonly User _user = new() { Id = Guid.NewGuid(), Name = "Ana", Email = Email, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Antiga#1a") };

    // In-memory stand-in for the code collection.
    private PasswordResetCode? _stored;
    private string? _sentText;

    public PasswordResetServiceTests()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(Email)).ReturnsAsync(_user);
        _codeRepo.Setup(r => r.GetByUserAsync(_user.Id)).ReturnsAsync(() => _stored);
        _codeRepo.Setup(r => r.ReplaceAsync(It.IsAny<PasswordResetCode>())).Callback((PasswordResetCode c) => _stored = c).Returns(Task.CompletedTask);
        _codeRepo.Setup(r => r.IncrementAttemptsAsync(It.IsAny<Guid>())).Callback(() => _stored!.Attempts++).Returns(Task.CompletedTask);
        _codeRepo.Setup(r => r.DeleteByUserAsync(_user.Id)).Callback(() => _stored = null).Returns(Task.CompletedTask);
        _email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback((string _, string _, string _, string text) => _sentText = text)
            .ReturnsAsync(true);
    }

    private PasswordResetService CreateService() =>
        new(new ResetPasswordRequestValidator(), _userRepo.Object, _codeRepo.Object, _refreshRepo.Object, _email.Object, _clock);

    private string SentCode() => Regex.Match(_sentText!, @"\b\d{6}\b").Value;

    private ResetPasswordRequest Reset(string code) =>
        new() { Email = Email, Code = code, NewPassword = NewPassword, ConfirmNewPassword = NewPassword };

    [Fact]
    public async Task Request_EmailsA6DigitCode_AndStoresOnlyItsHash()
    {
        await CreateService().RequestAsync(new ForgotPasswordRequest { Email = Email });

        var code = SentCode();
        Assert.Matches(@"^\d{6}$", code);
        Assert.NotNull(_stored);
        Assert.DoesNotContain(code, _stored!.CodeHash);
        Assert.Equal(_clock.UtcNow.AddMinutes(15), _stored.ExpiresAt);
    }

    [Fact]
    public async Task Request_UnknownEmail_DoesNothing()
    {
        await CreateService().RequestAsync(new ForgotPasswordRequest { Email = "ninguem@zeno.test" });

        _codeRepo.Verify(r => r.ReplaceAsync(It.IsAny<PasswordResetCode>()), Times.Never);
        _email.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Request_AccountWithoutPassword_DoesNothing()
    {
        _user.PasswordHash = null;

        await CreateService().RequestAsync(new ForgotPasswordRequest { Email = Email });

        _email.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Request_Twice_WithinCooldown_SendsOnce()
    {
        var service = CreateService();
        await service.RequestAsync(new ForgotPasswordRequest { Email = Email });
        _clock.UtcNow = _clock.UtcNow.AddSeconds(30);
        await service.RequestAsync(new ForgotPasswordRequest { Email = Email });

        _email.Verify(e => e.SendAsync(Email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Reset_WithTheRightCode_SetsPassword_ConsumesCode_AndEndsSessions()
    {
        var service = CreateService();
        await service.RequestAsync(new ForgotPasswordRequest { Email = Email });
        string? savedHash = null;
        _userRepo.Setup(r => r.UpdatePasswordAsync(_user.Id, It.IsAny<string>())).Callback((Guid _, string h) => savedHash = h).Returns(Task.CompletedTask);

        await service.ResetAsync(Reset(SentCode()));

        Assert.True(BCrypt.Net.BCrypt.Verify(NewPassword, savedHash));
        Assert.Null(_stored);
        _refreshRepo.Verify(r => r.RevokeAllUserTokensAsync(_user.Id), Times.Once);
    }

    [Fact]
    public async Task Reset_WithAWrongCode_CountsTheAttempt_AndKeepsThePassword()
    {
        var service = CreateService();
        await service.RequestAsync(new ForgotPasswordRequest { Email = Email });
        var wrong = SentCode() == "000000" ? "111111" : "000000";

        await Assert.ThrowsAsync<AppValidationException>(() => service.ResetAsync(Reset(wrong)));

        Assert.Equal(1, _stored!.Attempts);
        _userRepo.Verify(r => r.UpdatePasswordAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Reset_AfterExpiry_IsRejected()
    {
        var service = CreateService();
        await service.RequestAsync(new ForgotPasswordRequest { Email = Email });
        _clock.UtcNow = _clock.UtcNow.AddMinutes(16);

        await Assert.ThrowsAsync<AppValidationException>(() => service.ResetAsync(Reset(SentCode())));
        _userRepo.Verify(r => r.UpdatePasswordAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Reset_AfterTooManyAttempts_RejectsEvenTheRightCode()
    {
        var service = CreateService();
        await service.RequestAsync(new ForgotPasswordRequest { Email = Email });
        _stored!.Attempts = PasswordResetService.MaxAttempts;

        await Assert.ThrowsAsync<AppValidationException>(() => service.ResetAsync(Reset(SentCode())));
        _userRepo.Verify(r => r.UpdatePasswordAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Reset_WithAWeakPassword_IsRejectedBeforeCheckingTheCode()
    {
        var weak = new ResetPasswordRequest { Email = Email, Code = "123456", NewPassword = "fraca", ConfirmNewPassword = "fraca" };

        await Assert.ThrowsAsync<AppValidationException>(() => CreateService().ResetAsync(weak));
        _codeRepo.Verify(r => r.IncrementAttemptsAsync(It.IsAny<Guid>()), Times.Never);
    }

    private sealed class MutableClock : IClock
    {
        public DateTime UtcNow { get; set; }
    }
}
