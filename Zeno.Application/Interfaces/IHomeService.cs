using Zeno.Application.Requests.Homes;
using Zeno.Application.Responses;
using Zeno.Domain.Home;

namespace Zeno.Application.Interfaces;

public interface IHomeService
{
    Task<Home> CreateHome(Guid userId, CreateHomeRequest request);
    Task<Home> UpdateHome(Guid userId, UpdateHomeRequest request);
    Task<Home> DeleteHome(Guid userId, Guid id);
    Task<Home?> GetHomeById(Guid userId, Guid id);
    Task<IEnumerable<Home>> GetAllHomes(Guid userId);
}

public interface IHomeMemberService
{
    Task<IEnumerable<HomeMember>> GetMembers(Guid userId, Guid homeId);
    Task InviteMember(Guid adminUserId, Guid homeId, AddHomeMemberRequest request);
    Task RemoveMember(Guid adminUserId, Guid homeId, Guid memberUserId);
}

public interface IHomeExpenseService
{
    Task<HomeExpense> CreateExpense(Guid userId, CreateHomeExpenseRequest request);
    Task DeleteExpense(Guid userId, Guid homeId, Guid expenseId);
    Task<IEnumerable<HomeExpense>> GetExpensesByMonth(Guid userId, Guid homeId, int month, int year);
}

public interface IHomeSplitService
{
    Task<IEnumerable<ExpenseSplitResult>> CalculateExpenseSplit(Guid userId, Guid homeId, int month, int year);
    Task<IEnumerable<HomeWallet>> GetWallets(Guid userId, Guid homeId);
    Task AddWalletToHome(Guid userId, Guid homeId, Guid walletId);
    Task RemoveWalletFromHome(Guid adminUserId, Guid homeId, Guid walletId);
}

public interface IHomeBudgetService
{
    Task<BudgetAlertResponse> GetBudgetAlertAsync(Guid userId, Guid homeId, int month, int year);
}