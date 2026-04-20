using Dapper;
using Zeno.Domain.Entry;
using Zeno.Domain.Enum;
using Zeno.Domain.Home;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class HomeRepository : IHomeRepository
{
    private readonly ZenoDbContext _context;

    public HomeRepository(ZenoDbContext context)
    {
        _context = context;
    }

    public async Task<Home?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT Id, Name, Description, CreatedAt FROM Homes WHERE Id = @Id";
        return await _context.Connection.QueryFirstOrDefaultAsync<Home>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Home>> GetAllAsync()
    {
        const string sql = @"SELECT Id, Name, Description, CreatedAt FROM Homes ORDER BY CreatedAt DESC";
        return await _context.Connection.QueryAsync<Home>(sql);
    }

    public async Task<Home> CreateAsync(Home home)
    {
        const string sql = @"INSERT INTO Homes (Id, Name, Description, CreatedAt) VALUES (@Id, @Name, @Description, @CreatedAt)";
        await _context.Connection.ExecuteAsync(sql, new { home.Id, home.Name, home.Description, home.CreatedAt });
        return home;
    }

    public async Task<Home> UpdateAsync(Home home)
    {
        const string sql = @"UPDATE Homes SET Name = @Name, Description = @Description WHERE Id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { home.Id, home.Name, home.Description });
        return home;
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"DELETE FROM HomeExpenses WHERE HomeId = @Id;
                             DELETE FROM HomeWallets WHERE HomeId = @Id;
                             DELETE FROM Homes WHERE Id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task AddWalletAsync(Guid homeId, Guid walletId)
    {
        const string sql = @"IF NOT EXISTS (SELECT 1 FROM HomeWallets WHERE HomeId = @HomeId AND WalletId = @WalletId)
                             INSERT INTO HomeWallets (HomeId, WalletId) VALUES (@HomeId, @WalletId)";
        await _context.Connection.ExecuteAsync(sql, new { HomeId = homeId, WalletId = walletId });
    }

    public async Task RemoveWalletAsync(Guid homeId, Guid walletId)
    {
        const string sql = @"DELETE FROM HomeWallets WHERE HomeId = @HomeId AND WalletId = @WalletId";
        await _context.Connection.ExecuteAsync(sql, new { HomeId = homeId, WalletId = walletId });
    }

    public async Task<IEnumerable<HomeWallet>> GetWalletsByHomeAsync(Guid homeId)
    {
        const string sql = @"SELECT HomeId, WalletId FROM HomeWallets WHERE HomeId = @HomeId";
        return await _context.Connection.QueryAsync<HomeWallet>(sql, new { HomeId = homeId });
    }

    public async Task<HomeExpense> CreateExpenseAsync(HomeExpense expense)
    {
        const string sql = @"INSERT INTO HomeExpenses (Id, HomeId, Title, Value, Category, Month, Year, CreatedAt)
                             VALUES (@Id, @HomeId, @Title, @Value, @Category, @Month, @Year, @CreatedAt)";
        await _context.Connection.ExecuteAsync(sql, new
        {
            expense.Id,
            expense.HomeId,
            expense.Title,
            expense.Value,
            Category = (int)expense.Category,
            expense.Month,
            expense.Year,
            expense.CreatedAt
        });
        return expense;
    }

    public async Task DeleteExpenseAsync(Guid expenseId)
    {
        const string sql = @"DELETE FROM HomeExpenses WHERE Id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = expenseId });
    }

    public async Task<IEnumerable<HomeExpense>> GetExpensesByMonthAsync(Guid homeId, int month, int year)
    {
        const string sql = @"SELECT Id, HomeId, Title, Value, Category, Month, Year, CreatedAt 
                             FROM HomeExpenses WHERE HomeId = @HomeId AND Month = @Month AND Year = @Year";
        return await _context.Connection.QueryAsync<HomeExpense>(sql, new { HomeId = homeId, Month = month, Year = year });
    }

    public async Task<IEnumerable<ExpenseSplitResult>> CalculateSplitAsync(Guid homeId, int month, int year)
    {
        var homeWallets = await GetWalletsByHomeAsync(homeId);
        var walletIds = homeWallets.Select(hw => hw.WalletId).ToList();

        if (walletIds.Count == 0) return Enumerable.Empty<ExpenseSplitResult>();

        var expenses = await GetExpensesByMonthAsync(homeId, month, year);
        var totalExpenses = expenses.Sum(e => e.Value);

        if (totalExpenses == 0) return Enumerable.Empty<ExpenseSplitResult>();

        var incomes = await GetCreditsByWalletsAndMonthAsync(walletIds, month, year);
        var totalIncome = incomes.Sum(e => e.Value);

        if (totalIncome == 0) return Enumerable.Empty<ExpenseSplitResult>();

        var walletNames = await GetWalletNamesAsync(walletIds);

        var result = new List<ExpenseSplitResult>();
        foreach (var walletId in walletIds)
        {
            var walletIncome = incomes.Where(e => e.WalletId == walletId).Sum(e => e.Value);
            var percentage = totalIncome > 0 ? walletIncome / totalIncome : 0;

            result.Add(new ExpenseSplitResult
            {
                WalletId = walletId,
                WalletName = walletNames.GetValueOrDefault(walletId, "Desconhecida"),
                WalletIncome = walletIncome,
                TotalIncome = totalIncome,
                Percentage = Math.Round(percentage * 100, 2),
                AmountToPay = Math.Round(totalExpenses * percentage, 2)
            });
        }

        return result;
    }

    public async Task<IEnumerable<Entry>> GetCreditsByWalletsAndMonthAsync(IEnumerable<Guid> walletIds, int month, int year)
    {
        const string sql = @"SELECT Id, Title, Value, Type, Description, Category, Date, WalletId 
                             FROM Entries 
                             WHERE WalletId IN @WalletIds AND Type = 0 
                             AND MONTH(Date) = @Month AND YEAR(Date) = @Year";
        return await _context.Connection.QueryAsync<Entry>(sql, new { WalletIds = walletIds, Month = month, Year = year });
    }

    private async Task<Dictionary<Guid, string>> GetWalletNamesAsync(IEnumerable<Guid> walletIds)
    {
        const string sql = @"SELECT Id, Name FROM Wallets WHERE Id IN @WalletIds";
        var wallets = await _context.Connection.QueryAsync<(Guid Id, string Name)>(sql, new { WalletIds = walletIds });
        return wallets.ToDictionary(w => w.Id, w => w.Name);
    }
}
