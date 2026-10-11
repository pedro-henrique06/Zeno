using Moq;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Services;
using Zeno.Domain.Interfaces;
using UserEntity = Zeno.Domain.User.User;

namespace Zeno.Tests;

public class AccountDeletionServiceTests
{
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ICaptureKeyRepository> _captureKeyRepo = new();
    private readonly Mock<IWidgetKeyRepository> _widgetKeyRepo = new();
    private readonly Mock<IDeviceTokenRepository> _deviceTokenRepo = new();
    private readonly Mock<IPushSubscriptionRepository> _pushRepo = new();
    private readonly Mock<INotificationPreferenceRepository> _prefRepo = new();
    private readonly Mock<IHouseRepository> _houseRepo = new();
    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly List<string> _calls = new();

    public AccountDeletionServiceTests()
    {
        _userService.Setup(s => s.ResetAccount(_userId)).Callback(() => _calls.Add("reset")).Returns(Task.CompletedTask);
        _captureKeyRepo.Setup(r => r.DeleteByUserAsync(_userId)).Callback(() => _calls.Add("captureKey")).Returns(Task.CompletedTask);
        _widgetKeyRepo.Setup(r => r.DeleteByUserAsync(_userId)).Callback(() => _calls.Add("widgetKey")).Returns(Task.CompletedTask);
        _deviceTokenRepo.Setup(r => r.DeleteByUserAsync(_userId)).Callback(() => _calls.Add("devices")).Returns(Task.CompletedTask);
        _pushRepo.Setup(r => r.DeleteByUserAsync(_userId)).Callback(() => _calls.Add("push")).Returns(Task.CompletedTask);
        _prefRepo.Setup(r => r.DeleteByUserAsync(_userId)).Callback(() => _calls.Add("prefs")).Returns(Task.CompletedTask);
        _houseRepo.Setup(r => r.RemoveMemberFromAllAsync(_userId)).Callback(() => _calls.Add("leaveHouses")).Returns(Task.CompletedTask);
        _houseRepo.Setup(r => r.DeleteByOwnerAsync(_userId)).Callback(() => _calls.Add("ownedHouses")).Returns(Task.CompletedTask);
        _refreshRepo.Setup(r => r.DeleteByUserAsync(_userId)).Callback(() => _calls.Add("sessions")).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.DeleteAsync(_userId)).Callback(() => _calls.Add("user")).Returns(Task.CompletedTask);
    }

    private AccountDeletionService CreateService() =>
        new(_userService.Object, _userRepo.Object, _captureKeyRepo.Object, _widgetKeyRepo.Object, _deviceTokenRepo.Object,
            _pushRepo.Object, _prefRepo.Object, _houseRepo.Object, _refreshRepo.Object);

    [Fact]
    public async Task DeleteAccount_RemovesEverything_AndTheUserLast()
    {
        _userRepo.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(new UserEntity { Id = _userId });

        await CreateService().DeleteAccountAsync(_userId);

        Assert.Equal(
            new[] { "reset", "captureKey", "widgetKey", "devices", "push", "prefs", "leaveHouses", "ownedHouses", "sessions", "user" },
            _calls);
    }

    [Fact]
    public async Task DeleteAccount_UnknownUser_Throws_AndDeletesNothing()
    {
        _userRepo.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync((UserEntity?)null);

        await Assert.ThrowsAsync<AppValidationException>(() => CreateService().DeleteAccountAsync(_userId));

        Assert.Empty(_calls);
    }
}
