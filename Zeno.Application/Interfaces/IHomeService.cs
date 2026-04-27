using Zeno.Application.Responses;
using Zeno.Domain.Home;

namespace Zeno.Application.Interfaces;

public interface IHomeService
{
    Task<Home> CreateHome(Guid userId, Home home);
    Task<Home> UpdateHome(Guid userId, Home home);
    Task<Home> DeleteHome(Guid userId, Guid id);
    Task<Home?> GetHomeById(Guid userId, Guid id);
    Task<IEnumerable<Home>> GetAllHomes(Guid userId);
    Task AddWalletToHome(Guid userId, Guid homeId, Guid walletId);
    Task RemoveWalletFromHome(Guid userId, Guid homeId, Guid walletId);
    Task<HomeExpense> CreateExpense(Guid userId, HomeExpense expense);
    Task DeleteExpense(Guid userId, Guid expenseId);
    Task<IEnumerable<HomeExpense>> GetExpensesByMonth(Guid userId, Guid homeId, int month, int year);
    Task<IEnumerable<ExpenseSplitResult>> CalculateExpenseSplit(Guid userId, Guid homeId, int month, int year);
    Task InviteMember(Guid adminUserId, Guid homeId, Guid invitedUserId);
    Task RemoveMember(Guid adminUserId, Guid homeId, Guid memberUserId);
    Task<IEnumerable<HomeMember>> GetMembers(Guid userId, Guid homeId);
    Task<BudgetAlertResponse> GetBudgetAlertAsync(Guid userId, Guid homeId, int month, int year);
}
