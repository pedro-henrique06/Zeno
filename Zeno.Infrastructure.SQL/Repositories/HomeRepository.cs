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
        const string sql = @"SELECT Id, Name, Description, SplitMode, CreatedAt FROM Homes WHERE Id = @Id";
        return await _context.Connection.QueryFirstOrDefaultAsync<Home>(sql, new { Id = id });
    }

    public async Task<Home?> GetByIdAndMemberAsync(Guid id, Guid userId)
    {
        const string sql = @"SELECT h.Id, h.Name, h.Description, h.SplitMode, h.CreatedAt 
                             FROM Homes h 
                             INNER JOIN HomeMembers hm ON h.Id = hm.HomeId 
                             WHERE h.Id = @Id AND hm.UserId = @UserId";
        return await _context.Connection.QueryFirstOrDefaultAsync<Home>(sql, new { Id = id, UserId = userId });
    }

    public async Task<IEnumerable<Home>> GetAllByUserAsync(Guid userId)
    {
        const string sql = @"SELECT h.Id, h.Name, h.Description, h.SplitMode, h.CreatedAt 
                             FROM Homes h 
                             INNER JOIN HomeMembers hm ON h.Id = hm.HomeId 
                             WHERE hm.UserId = @UserId 
                             ORDER BY h.CreatedAt DESC";
        return await _context.Connection.QueryAsync<Home>(sql, new { UserId = userId });
    }

    public async Task<Home> CreateAsync(Home home)
    {
        const string sql = @"INSERT INTO Homes (Id, Name, Description, SplitMode, CreatedAt) 
                             VALUES (@Id, @Name, @Description, @SplitMode, @CreatedAt)";
        await _context.Connection.ExecuteAsync(sql, new { home.Id, home.Name, home.Description, SplitMode = (int)home.SplitMode, home.CreatedAt });
        return home;
    }

    public async Task<Home> UpdateAsync(Home home)
    {
        const string sql = @"UPDATE Homes SET Name = @Name, Description = @Description, SplitMode = @SplitMode WHERE Id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { home.Id, home.Name, home.Description, SplitMode = (int)home.SplitMode });
        return home;
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"DELETE FROM HomeExpenses WHERE HomeId = @Id;
                             DELETE FROM HomeWallets WHERE HomeId = @Id;
                             DELETE FROM HomeMembers WHERE HomeId = @Id;
                             DELETE FROM Homes WHERE Id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task AddWalletAsync(Guid homeId, Guid walletId)
    {
        const string sql = @"INSERT INTO HomeWallets (HomeId, WalletId) VALUES (@HomeId, @WalletId) ON CONFLICT DO NOTHING";
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
        var home = await GetByIdAsync(homeId);
        if (home is null) return Enumerable.Empty<ExpenseSplitResult>();

        var homeWallets = await GetWalletsByHomeAsync(homeId);
        var walletIds = homeWallets.Select(hw => hw.WalletId).ToList();

        if (walletIds.Count == 0) return Enumerable.Empty<ExpenseSplitResult>();

        var expenses = await GetExpensesByMonthAsync(homeId, month, year);
        var totalExpenses = expenses.Sum(e => e.Value);

        if (totalExpenses == 0) return Enumerable.Empty<ExpenseSplitResult>();

        var incomes = await GetCreditsByWalletsAndMonthAsync(walletIds, month, year);
        var totalIncome = incomes.Sum(e => e.Value);

        var walletNames = await GetWalletNamesAsync(walletIds);
        var walletUsers = await GetWalletUsersAsync(walletIds);
        var userNames = await GetUserNamesAsync(walletUsers.Values);
        var memberSalaries = await GetMemberSalariesAsync(walletIds);

        if (home.SplitMode == SplitMode.ByIndividualAccounts)
        {
            return CalculateSplitBySalary(walletIds, walletNames, walletUsers, userNames, memberSalaries, totalExpenses);
        }

        return CalculateSplitByIncome(walletIds, walletNames, walletUsers, userNames, incomes, totalIncome, totalExpenses, memberSalaries);
    }

    private List<ExpenseSplitResult> CalculateSplitBySalary(
        List<Guid> walletIds,
        Dictionary<Guid, string> walletNames,
        Dictionary<Guid, Guid> walletUsers,
        Dictionary<Guid, string> userNames,
        Dictionary<Guid, decimal> memberSalaries,
        decimal totalExpenses)
    {
        var totalSalary = memberSalaries.Values.Sum();
        if (totalSalary == 0) return new List<ExpenseSplitResult>();

        var result = new List<ExpenseSplitResult>();
        foreach (var walletId in walletIds)
        {
            var userId = walletUsers.GetValueOrDefault(walletId);
            var salary = memberSalaries.GetValueOrDefault(walletId, 0);
            var weight = salary / totalSalary;

            result.Add(new ExpenseSplitResult
            {
                WalletId = walletId,
                UserId = userId,
                UserName = userNames.GetValueOrDefault(userId, "Desconhecido"),
                WalletName = walletNames.GetValueOrDefault(walletId, "Desconhecida"),
                SalaryAmount = salary,
                SalaryWeight = Math.Round(weight * 100, 2),
                TotalSalary = totalSalary,
                Percentage = Math.Round(weight * 100, 2),
                AmountToPay = Math.Round(totalExpenses * weight, 2)
            });
        }

        return result;
    }

    private List<ExpenseSplitResult> CalculateSplitByIncome(
        List<Guid> walletIds,
        Dictionary<Guid, string> walletNames,
        Dictionary<Guid, Guid> walletUsers,
        Dictionary<Guid, string> userNames,
        IEnumerable<Entry> incomes,
        decimal totalIncome,
        decimal totalExpenses,
        Dictionary<Guid, decimal> memberSalaries)
    {
        if (totalIncome == 0) return new List<ExpenseSplitResult>();

        var result = new List<ExpenseSplitResult>();
        foreach (var walletId in walletIds)
        {
            var walletIncome = incomes.Where(e => e.WalletId == walletId).Sum(e => e.Value);
            var userId = walletUsers.GetValueOrDefault(walletId);
            var percentage = totalIncome > 0 ? walletIncome / totalIncome : 0;
            var salary = memberSalaries.GetValueOrDefault(walletId, 0);

            result.Add(new ExpenseSplitResult
            {
                WalletId = walletId,
                UserId = userId,
                UserName = userNames.GetValueOrDefault(userId, "Desconhecido"),
                WalletName = walletNames.GetValueOrDefault(walletId, "Desconhecida"),
                WalletIncome = walletIncome,
                SalaryAmount = salary,
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
                             AND EXTRACT(MONTH FROM Date) = @Month AND EXTRACT(YEAR FROM Date) = @Year";
        return await _context.Connection.QueryAsync<Entry>(sql, new { WalletIds = walletIds, Month = month, Year = year });
    }

    public async Task AddMemberAsync(Guid homeId, Guid userId, int role)
    {
        const string sql = @"INSERT INTO HomeMembers (HomeId, UserId, Role, JoinedAt) VALUES (@HomeId, @UserId, @Role, @JoinedAt) ON CONFLICT DO NOTHING";
        await _context.Connection.ExecuteAsync(sql, new { HomeId = homeId, UserId = userId, Role = role, JoinedAt = DateTime.UtcNow });
    }

    public async Task RemoveMemberAsync(Guid homeId, Guid userId)
    {
        const string sql = @"DELETE FROM HomeMembers WHERE HomeId = @HomeId AND UserId = @UserId";
        await _context.Connection.ExecuteAsync(sql, new { HomeId = homeId, UserId = userId });
    }

    public async Task<bool> IsMemberAsync(Guid homeId, Guid userId)
    {
        const string sql = @"SELECT COUNT(1) FROM HomeMembers WHERE HomeId = @HomeId AND UserId = @UserId";
        return await _context.Connection.ExecuteScalarAsync<int>(sql, new { HomeId = homeId, UserId = userId }) > 0;
    }

    public async Task<bool> IsAdminAsync(Guid homeId, Guid userId)
    {
        const string sql = @"SELECT COUNT(1) FROM HomeMembers WHERE HomeId = @HomeId AND UserId = @UserId AND Role = 0";
        return await _context.Connection.ExecuteScalarAsync<int>(sql, new { HomeId = homeId, UserId = userId }) > 0;
    }

    public async Task<IEnumerable<HomeMember>> GetMembersByHomeAsync(Guid homeId)
    {
        const string sql = @"SELECT HomeId, UserId, Role, JoinedAt FROM HomeMembers WHERE HomeId = @HomeId";
        return await _context.Connection.QueryAsync<HomeMember>(sql, new { HomeId = homeId });
    }

    public async Task<IEnumerable<Home>> GetHomesByUserAsync(Guid userId)
    {
        return await GetAllByUserAsync(userId);
    }

    public async Task<decimal> GetTotalIncomeAsync(Guid homeId, int month, int year)
    {
        var homeWallets = await GetWalletsByHomeAsync(homeId);
        var walletIds = homeWallets.Select(hw => hw.WalletId).ToList();

        if (walletIds.Count == 0) return 0;

        var incomes = await GetCreditsByWalletsAndMonthAsync(walletIds, month, year);
        return incomes.Sum(e => e.Value);
    }

    public async Task<decimal> GetTotalExpensesAsync(Guid homeId, int month, int year)
    {
        var expenses = await GetExpensesByMonthAsync(homeId, month, year);
        return expenses.Sum(e => e.Value);
    }

    private async Task<Dictionary<Guid, string>> GetWalletNamesAsync(IEnumerable<Guid> walletIds)
    {
        const string sql = @"SELECT Id, Name FROM Wallets WHERE Id IN @WalletIds";
        var wallets = await _context.Connection.QueryAsync<(Guid Id, string Name)>(sql, new { WalletIds = walletIds });
        return wallets.ToDictionary(w => w.Id, w => w.Name);
    }

    private async Task<Dictionary<Guid, Guid>> GetWalletUsersAsync(IEnumerable<Guid> walletIds)
    {
        const string sql = @"SELECT Id, UserId FROM Wallets WHERE Id IN @WalletIds";
        var wallets = await _context.Connection.QueryAsync<(Guid Id, Guid UserId)>(sql, new { WalletIds = walletIds });
        return wallets.ToDictionary(w => w.Id, w => w.UserId);
    }

    private async Task<Dictionary<Guid, string>> GetUserNamesAsync(IEnumerable<Guid> userIds)
    {
        var ids = userIds.Distinct().ToList();
        const string sql = @"SELECT Id, Name FROM Users WHERE Id IN @UserIds";
        var users = await _context.Connection.QueryAsync<(Guid Id, string Name)>(sql, new { UserIds = ids });
        return users.ToDictionary(u => u.Id, u => u.Name);
    }

    private async Task<Dictionary<Guid, decimal>> GetMemberSalariesAsync(IEnumerable<Guid> walletIds)
    {
        const string sql = @"SELECT WalletId, SUM(Amount) as TotalAmount 
                             FROM Salaries 
                             WHERE WalletId IN @WalletIds AND Active = true 
                             GROUP BY WalletId";
        var salaries = await _context.Connection.QueryAsync<(Guid WalletId, decimal TotalAmount)>(sql, new { WalletIds = walletIds });
        return salaries.ToDictionary(s => s.WalletId, s => s.TotalAmount);
    }
}
