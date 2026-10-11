using Moq;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Services;
using Zeno.Domain.Interfaces;
using UserEntity = Zeno.Domain.User.User;

namespace Zeno.Tests;

public class UserServiceResetAccountTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IEntryRepository> _entryRepo = new();
    private readonly Mock<IMonthlyExpenseCategoryRepository> _categoryRepo = new();
    private readonly Mock<ITagRepository> _tagRepo = new();
    private readonly Mock<IGoalRepository> _goalRepo = new();
    private readonly Mock<ICaptureRuleRepository> _ruleRepo = new();
    private readonly Guid _userId = Guid.NewGuid();

    private UserService CreateService() =>
        new(
            Mock.Of<IServiceProvider>(),
            _userRepo.Object,
            _entryRepo.Object,
            _categoryRepo.Object,
            _tagRepo.Object,
            _goalRepo.Object,
            _ruleRepo.Object,
            Mock.Of<IExchangeRateService>());

    [Fact]
    public async Task ResetAccount_WipesFinancialData_AndZeroesDailyBudget()
    {
        var user = new UserEntity { Id = _userId, DailyBudget = 85m };
        _userRepo.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateProfileAsync(It.IsAny<UserEntity>())).ReturnsAsync((UserEntity u) => u);

        await CreateService().ResetAccount(_userId);

        _entryRepo.Verify(r => r.DeleteByUserAsync(_userId), Times.Once);
        _tagRepo.Verify(r => r.DeleteByUserAsync(_userId), Times.Once);
        _categoryRepo.Verify(r => r.DeleteByUserAsync(_userId), Times.Once);
        _goalRepo.Verify(r => r.DeleteByUserAsync(_userId), Times.Once);
        _ruleRepo.Verify(r => r.DeleteByUserAsync(_userId), Times.Once);
        _userRepo.Verify(r => r.UpdateProfileAsync(It.Is<UserEntity>(u => u.Id == _userId && u.DailyBudget == 0)), Times.Once);
    }

    [Fact]
    public async Task ResetAccount_UnknownUser_Throws_AndDeletesNothing()
    {
        _userRepo.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync((UserEntity?)null);

        await Assert.ThrowsAsync<AppValidationException>(() => CreateService().ResetAccount(_userId));

        _entryRepo.Verify(r => r.DeleteByUserAsync(It.IsAny<Guid>()), Times.Never);
        _tagRepo.Verify(r => r.DeleteByUserAsync(It.IsAny<Guid>()), Times.Never);
        _categoryRepo.Verify(r => r.DeleteByUserAsync(It.IsAny<Guid>()), Times.Never);
        _goalRepo.Verify(r => r.DeleteByUserAsync(It.IsAny<Guid>()), Times.Never);
        _ruleRepo.Verify(r => r.DeleteByUserAsync(It.IsAny<Guid>()), Times.Never);
    }
}
