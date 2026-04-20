using Zeno.Domain.Home;

namespace Zeno.Application.Interfaces;

public interface IHomeService
{
    Task<Home> CreateHome(Home home);
    Task<Home> UpdateHome(Home home);
    Task<Home> DeleteHome(Guid id);
    Task<Home?> GetHomeById(Guid id);
    Task<IEnumerable<Home>> GetAllHomes();
    Task AddWalletToHome(Guid homeId, Guid walletId);
    Task RemoveWalletFromHome(Guid homeId, Guid walletId);
    Task<HomeExpense> CreateExpense(HomeExpense expense);
    Task DeleteExpense(Guid expenseId);
    Task<IEnumerable<HomeExpense>> GetExpensesByMonth(Guid homeId, int month, int year);
    Task<IEnumerable<ExpenseSplitResult>> CalculateExpenseSplit(Guid homeId, int month, int year);
}
