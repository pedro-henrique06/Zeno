using Moq;
using FluentValidation;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Requests.Entries;
using Zeno.Application.Services;
using Zeno.Domain.Entry;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;

namespace Zeno.Tests;

public class EntryServiceTests
{
    private readonly Mock<IEntryRepository> _entryRepoMock;
    private readonly Mock<IWalletRepository> _walletRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICategoryRuleService> _categoryRuleServiceMock;
    private readonly Mock<IValidator<CreateEntryRequest>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateEntryRequest>> _updateValidatorMock;
    private readonly Mock<IValidator<DeleteEntryRequest>> _deleteValidatorMock;
    private readonly Mock<IValidator<GetEntriesByMonthQuery>> _getEntriesValidatorMock;

    public EntryServiceTests()
    {
        _entryRepoMock = new Mock<IEntryRepository>();
        _walletRepoMock = new Mock<IWalletRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _categoryRuleServiceMock = new Mock<ICategoryRuleService>();
        _createValidatorMock = new Mock<IValidator<CreateEntryRequest>>();
        _updateValidatorMock = new Mock<IValidator<UpdateEntryRequest>>();
        _deleteValidatorMock = new Mock<IValidator<DeleteEntryRequest>>();
        _getEntriesValidatorMock = new Mock<IValidator<GetEntriesByMonthQuery>>();

        _createValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateEntryRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _updateValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<UpdateEntryRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _deleteValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<DeleteEntryRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _getEntriesValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<GetEntriesByMonthQuery>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
    }

    private EntryService CreateService()
    {
        return new EntryService(
            _createValidatorMock.Object,
            _updateValidatorMock.Object,
            _deleteValidatorMock.Object,
            _getEntriesValidatorMock.Object,
            _entryRepoMock.Object,
            _walletRepoMock.Object,
            _unitOfWorkMock.Object,
            _categoryRuleServiceMock.Object);
    }

    [Fact]
    public async Task CreateEntry_CreditIncreasesBalance()
    {
        var userId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var wallet = new Zeno.Domain.Wallet.Wallet { Id = walletId, UserId = userId, Balance = 100 };
        var request = new CreateEntryRequest
        {
            Title = "Salary",
            Value = 5000,
            Type = EntryType.Credit,
            WalletId = walletId,
            Date = DateTime.UtcNow
        };

        _walletRepoMock.Setup(r => r.GetByIdAndUserAsync(walletId, userId)).ReturnsAsync(wallet);
        _entryRepoMock.Setup(r => r.CreateAsync(It.IsAny<Entry>(), It.IsAny<object>())).ReturnsAsync((Entry e, object _) => e);
        _walletRepoMock.Setup(r => r.AddBalanceAsync(walletId, It.IsAny<decimal>(), It.IsAny<object>())).Returns(Task.CompletedTask);

        var service = CreateService();
        var result = await service.CreateEntry(userId, request);

        Assert.Equal(5000, result.Value);
        Assert.Equal(EntryType.Credit, result.Type);
        _walletRepoMock.Verify(r => r.AddBalanceAsync(walletId, 5000, It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task CreateEntry_DebitDecreasesBalance()
    {
        var userId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var wallet = new Zeno.Domain.Wallet.Wallet { Id = walletId, UserId = userId, Balance = 1000 };
        var request = new CreateEntryRequest
        {
            Title = "Grocery",
            Value = 200,
            Type = EntryType.Debit,
            WalletId = walletId,
            Date = DateTime.UtcNow
        };

        _walletRepoMock.Setup(r => r.GetByIdAndUserAsync(walletId, userId)).ReturnsAsync(wallet);
        _entryRepoMock.Setup(r => r.CreateAsync(It.IsAny<Entry>(), It.IsAny<object>())).ReturnsAsync((Entry e, object _) => e);
        _walletRepoMock.Setup(r => r.AddBalanceAsync(walletId, It.IsAny<decimal>(), It.IsAny<object>())).Returns(Task.CompletedTask);

        var service = CreateService();
        var result = await service.CreateEntry(userId, request);

        Assert.Equal(200, result.Value);
        Assert.Equal(EntryType.Debit, result.Type);
        _walletRepoMock.Verify(r => r.AddBalanceAsync(walletId, -200, It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task CreateEntry_WalletNotFoundThrows()
    {
        var userId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var request = new CreateEntryRequest
        {
            Title = "Test",
            Value = 100,
            Type = EntryType.Credit,
            WalletId = walletId
        };

        _walletRepoMock.Setup(r => r.GetByIdAndUserAsync(walletId, userId)).ReturnsAsync((Zeno.Domain.Wallet.Wallet?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<Zeno.Application.Exceptions.AppValidationException>(() =>
            service.CreateEntry(userId, request));
    }

    [Fact]
    public async Task DeleteEntry_ReversesBalance()
    {
        var userId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var existingEntry = new Entry
        {
            Id = entryId,
            WalletId = walletId,
            Value = 300,
            Type = EntryType.Credit
        };
        var wallet = new Zeno.Domain.Wallet.Wallet { Id = walletId, UserId = userId, Balance = 1300 };
        var request = new DeleteEntryRequest { Id = entryId };

        _entryRepoMock.Setup(r => r.GetByIdAsync(entryId)).ReturnsAsync(existingEntry);
        _walletRepoMock.Setup(r => r.GetByIdAndUserAsync(walletId, userId)).ReturnsAsync(wallet);

        var service = CreateService();
        var result = await service.DeleteEntry(userId, request);

        _walletRepoMock.Verify(r => r.AddBalanceAsync(walletId, -300, It.IsAny<object>()), Times.Once);
    }
}