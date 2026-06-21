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
        const string sql = @"SELECT id, name, description, splitmode, createdat FROM homes WHERE id = @Id";
        return await _context.Connection.QueryFirstOrDefaultAsync<Home>(sql, new { Id = id });
    }

    public async Task<Home?> GetByIdAndMemberAsync(Guid id, Guid userId)
    {
        const string sql = @"SELECT h.id, h.name, h.description, h.splitmode, h.createdat
                             FROM homes h
                             INNER JOIN homemembers hm ON h.id = hm.homeid
                             WHERE h.id = @Id AND hm.userid = @UserId";
        return await _context.Connection.QueryFirstOrDefaultAsync<Home>(sql, new { Id = id, UserId = userId });
    }

    public async Task<IEnumerable<Home>> GetAllByUserAsync(Guid userId)
    {
        const string sql = @"SELECT h.id, h.name, h.description, h.splitmode, h.createdat
                             FROM homes h
                             INNER JOIN homemembers hm ON h.id = hm.homeid
                             WHERE hm.userid = @UserId
                             ORDER BY h.createdat DESC";
        return await _context.Connection.QueryAsync<Home>(sql, new { UserId = userId });
    }

    public async Task<Home> CreateAsync(Home home)
    {
        const string sql = @"INSERT INTO homes (id, name, description, splitmode, createdat)
                             VALUES (@Id, @Name, @Description, @SplitMode, @CreatedAt)";
        await _context.Connection.ExecuteAsync(sql, new { home.Id, home.Name, home.Description, SplitMode = (int)home.SplitMode, home.CreatedAt });
        return home;
    }

    public async Task<Home> UpdateAsync(Home home)
    {
        const string sql = @"UPDATE homes SET name = @Name, description = @Description, splitmode = @SplitMode WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { home.Id, home.Name, home.Description, SplitMode = (int)home.SplitMode });
        return home;
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"DELETE FROM home_expenses WHERE homeid = @Id;
                             DELETE FROM home_wallets WHERE homeid = @Id;
                             DELETE FROM homemembers WHERE homeid = @Id;
                             DELETE FROM homes WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task AddWalletAsync(Guid homeId, Guid walletId)
    {
        const string sql = @"INSERT IGNORE INTO home_wallets (homeid, walletid) VALUES (@HomeId, @WalletId)";
        await _context.Connection.ExecuteAsync(sql, new { HomeId = homeId, WalletId = walletId });
    }

    public async Task RemoveWalletAsync(Guid homeId, Guid walletId)
    {
        const string sql = @"DELETE FROM home_wallets WHERE homeid = @HomeId AND walletid = @WalletId";
        await _context.Connection.ExecuteAsync(sql, new { HomeId = homeId, WalletId = walletId });
    }

    public async Task<IEnumerable<HomeWallet>> GetWalletsByHomeAsync(Guid homeId)
    {
        const string sql = @"SELECT homeid, walletid FROM home_wallets WHERE homeid = @HomeId";
        return await _context.Connection.QueryAsync<HomeWallet>(sql, new { HomeId = homeId });
    }

    public async Task<HomeExpense> CreateExpenseAsync(HomeExpense expense)
    {
        const string sql = @"INSERT INTO home_expenses (id, homeid, title, value, category, month, year, created_at)
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
        const string sql = @"DELETE FROM home_expenses WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = expenseId });
    }

    public async Task<IEnumerable<HomeExpense>> GetExpensesByMonthAsync(Guid homeId, int month, int year)
    {
        const string sql = @"SELECT id, homeid, title, value, category, month, year, created_at
                             FROM home_expenses WHERE homeid = @HomeId AND month = @Month AND year = @Year";
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
        const string sql = @"SELECT id, title, value, type, description, category, date, walletid
                             FROM entries
                             WHERE walletid IN @WalletIds AND type = 0
                             AND MONTH(date) = @Month AND YEAR(date) = @Year";
        return await _context.Connection.QueryAsync<Entry>(sql, new { WalletIds = walletIds, Month = month, Year = year });
    }

    public async Task AddMemberAsync(Guid homeId, Guid userId, int role)
    {
        const string sql = @"INSERT IGNORE INTO homemembers (homeid, userid, role, joinedat) VALUES (@HomeId, @UserId, @Role, @JoinedAt)";
        await _context.Connection.ExecuteAsync(sql, new { HomeId = homeId, UserId = userId, Role = role, JoinedAt = DateTime.UtcNow });
    }

    public async Task RemoveMemberAsync(Guid homeId, Guid userId)
    {
        const string sql = @"DELETE FROM homemembers WHERE homeid = @HomeId AND userid = @UserId";
        await _context.Connection.ExecuteAsync(sql, new { HomeId = homeId, UserId = userId });
    }

    public async Task<bool> IsMemberAsync(Guid homeId, Guid userId)
    {
        const string sql = @"SELECT COUNT(1) FROM homemembers WHERE homeid = @HomeId AND userid = @UserId";
        return await _context.Connection.ExecuteScalarAsync<int>(sql, new { HomeId = homeId, UserId = userId }) > 0;
    }

    public async Task<bool> IsAdminAsync(Guid homeId, Guid userId)
    {
        const string sql = @"SELECT COUNT(1) FROM homemembers WHERE homeid = @HomeId AND userid = @UserId AND role = 0";
        return await _context.Connection.ExecuteScalarAsync<int>(sql, new { HomeId = homeId, UserId = userId }) > 0;
    }

    public async Task<IEnumerable<HomeMember>> GetMembersByHomeAsync(Guid homeId)
    {
        const string sql = @"SELECT hm.homeid, hm.userid, u.name as user_name, u.email as user_email, hm.role, hm.joinedat
                             FROM homemembers hm
                             INNER JOIN users u ON hm.userid = u.id
                             WHERE hm.homeid = @HomeId";
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
        const string sql = @"SELECT id, name FROM wallets WHERE id IN @WalletIds";
        var wallets = await _context.Connection.QueryAsync<(Guid Id, string Name)>(sql, new { WalletIds = walletIds });
        return wallets.ToDictionary(w => w.Id, w => w.Name);
    }

    private async Task<Dictionary<Guid, Guid>> GetWalletUsersAsync(IEnumerable<Guid> walletIds)
    {
        const string sql = @"SELECT id, userid FROM wallets WHERE id IN @WalletIds";
        var wallets = await _context.Connection.QueryAsync<(Guid Id, Guid UserId)>(sql, new { WalletIds = walletIds });
        return wallets.ToDictionary(w => w.Id, w => w.UserId);
    }

    private async Task<Dictionary<Guid, string>> GetUserNamesAsync(IEnumerable<Guid> userIds)
    {
        var ids = userIds.Distinct().ToList();
        const string sql = @"SELECT id, name FROM users WHERE id IN @UserIds";
        var users = await _context.Connection.QueryAsync<(Guid Id, string Name)>(sql, new { UserIds = ids });
        return users.ToDictionary(u => u.Id, u => u.Name);
    }

    private async Task<Dictionary<Guid, decimal>> GetMemberSalariesAsync(IEnumerable<Guid> walletIds)
    {
        const string sql = @"SELECT walletid, SUM(value) as total_amount
                             FROM recurrententries
                             WHERE walletid IN @WalletIds AND isactive = 1 AND kind = 0
                             GROUP BY walletid";
        var salaries = await _context.Connection.QueryAsync<(Guid WalletId, decimal TotalAmount)>(sql, new { WalletIds = walletIds });
        return salaries.ToDictionary(s => s.WalletId, s => s.TotalAmount);
    }
}