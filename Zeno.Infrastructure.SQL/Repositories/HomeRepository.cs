using MongoDB.Driver;
using Zeno.Domain.Entry;
using Zeno.Domain.Enum;
using Zeno.Domain.Home;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Wallet;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class HomeRepository : IHomeRepository
{
    private readonly ZenoMongoContext _context;

    public HomeRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<Home?> GetByIdAsync(Guid id)
    {
        return await _context.Homes.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Home?> GetByIdAndMemberAsync(Guid id, Guid userId)
    {
        var home = await _context.Homes.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (home is null) return null;

        var isMember = await _context.HomeMembers.Find(x => x.HomeId == id && x.UserId == userId).AnyAsync();
        return isMember ? home : null;
    }

    public async Task<IEnumerable<Home>> GetAllByUserAsync(Guid userId)
    {
        var memberIds = await _context.HomeMembers
            .Find(x => x.UserId == userId)
            .Project(x => x.HomeId)
            .ToListAsync();

        return await _context.Homes
            .Find(x => memberIds.Contains(x.Id))
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<Home> CreateAsync(Home home)
    {
        await _context.Homes.InsertOneAsync(home);
        return home;
    }

    public async Task<Home> UpdateAsync(Home home)
    {
        var filter = Builders<Home>.Filter.Eq(x => x.Id, home.Id);
        await _context.Homes.ReplaceOneAsync(filter, home);
        return home;
    }

    public async Task DeleteAsync(Guid id)
    {
        // Delete related data first
        await _context.HomeExpenses.DeleteManyAsync(x => x.HomeId == id);
        await _context.HomeWallets.DeleteManyAsync(x => x.HomeId == id);
        await _context.HomeMembers.DeleteManyAsync(x => x.HomeId == id);
        await _context.Homes.DeleteOneAsync(x => x.Id == id);
    }

    public async Task AddWalletAsync(Guid homeId, Guid walletId)
    {
        var existing = await _context.HomeWallets
            .Find(x => x.HomeId == homeId && x.WalletId == walletId)
            .FirstOrDefaultAsync();

        if (existing is null)
        {
            await _context.HomeWallets.InsertOneAsync(new HomeWallet
            {
                Id = Guid.NewGuid(),
                HomeId = homeId,
                WalletId = walletId
            });
        }
    }

    public async Task RemoveWalletAsync(Guid homeId, Guid walletId)
    {
        var filter = Builders<HomeWallet>.Filter.Eq(x => x.HomeId, homeId) &
                     Builders<HomeWallet>.Filter.Eq(x => x.WalletId, walletId);
        await _context.HomeWallets.DeleteOneAsync(filter);
    }

    public async Task<IEnumerable<HomeWallet>> GetWalletsByHomeAsync(Guid homeId)
    {
        return await _context.HomeWallets
            .Find(x => x.HomeId == homeId)
            .ToListAsync();
    }

    public async Task<HomeExpense> CreateExpenseAsync(HomeExpense expense)
    {
        await _context.HomeExpenses.InsertOneAsync(expense);
        return expense;
    }

    public async Task DeleteExpenseAsync(Guid expenseId)
    {
        await _context.HomeExpenses.DeleteOneAsync(x => x.Id == expenseId);
    }

    public async Task<IEnumerable<HomeExpense>> GetExpensesByMonthAsync(Guid homeId, int month, int year)
    {
        return await _context.HomeExpenses
            .Find(x => x.HomeId == homeId && x.Month == month && x.Year == year)
            .ToListAsync();
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
            var weight = totalSalary > 0 ? salary / totalSalary : 0;

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
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);
        var walletIdList = walletIds.ToList();

        return await _context.Entries
            .Find(x => walletIdList.Contains(x.WalletId) &&
                       x.Type == EntryType.Credit &&
                       x.Date >= startDate &&
                       x.Date < endDate)
            .ToListAsync();
    }

    public async Task AddMemberAsync(Guid homeId, Guid userId, int role)
    {
        var existing = await _context.HomeMembers
            .Find(x => x.HomeId == homeId && x.UserId == userId)
            .FirstOrDefaultAsync();

        if (existing is null)
        {
            await _context.HomeMembers.InsertOneAsync(new HomeMember
            {
                Id = Guid.NewGuid(),
                HomeId = homeId,
                UserId = userId,
                Role = (HomeRole)role,
                JoinedAt = DateTime.UtcNow
            });
        }
    }

    public async Task RemoveMemberAsync(Guid homeId, Guid userId)
    {
        var filter = Builders<HomeMember>.Filter.Eq(x => x.HomeId, homeId) &
                     Builders<HomeMember>.Filter.Eq(x => x.UserId, userId);
        await _context.HomeMembers.DeleteOneAsync(filter);
    }

    public async Task<bool> IsMemberAsync(Guid homeId, Guid userId)
    {
        return await _context.HomeMembers
            .Find(x => x.HomeId == homeId && x.UserId == userId)
            .AnyAsync();
    }

    public async Task<bool> IsAdminAsync(Guid homeId, Guid userId)
    {
        return await _context.HomeMembers
            .Find(x => x.HomeId == homeId && x.UserId == userId && x.Role == HomeRole.Admin)
            .AnyAsync();
    }

    public async Task<IEnumerable<HomeMember>> GetMembersByHomeAsync(Guid homeId)
    {
        return await _context.HomeMembers
            .Find(x => x.HomeId == homeId)
            .ToListAsync();
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
        var walletIdList = walletIds.ToList();
        var filter = Builders<Wallet>.Filter.In(x => x.Id, walletIdList);
        var wallets = await _context.Wallets
            .Find(filter)
            .Project(x => new { x.Id, x.Name })
            .ToListAsync();
        return wallets.ToDictionary(w => w.Id, w => w.Name);
    }

    private async Task<Dictionary<Guid, Guid>> GetWalletUsersAsync(IEnumerable<Guid> walletIds)
    {
        var walletIdList = walletIds.ToList();
        var filter = Builders<Wallet>.Filter.In(x => x.Id, walletIdList);
        var wallets = await _context.Wallets
            .Find(filter)
            .Project(x => new { x.Id, x.UserId })
            .ToListAsync();
        return wallets.ToDictionary(w => w.Id, w => w.UserId);
    }

    private async Task<Dictionary<Guid, string>> GetUserNamesAsync(IEnumerable<Guid> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (!ids.Any()) return new Dictionary<Guid, string>();

        var users = await _context.Users
            .Find(x => ids.Contains(x.Id))
            .Project(x => new { x.Id, x.Name })
            .ToListAsync();
        return users.ToDictionary(u => u.Id, u => u.Name);
    }

    private async Task<Dictionary<Guid, decimal>> GetMemberSalariesAsync(IEnumerable<Guid> walletIds)
    {
        var walletIdList = walletIds.ToList();
        if (!walletIdList.Any()) return new Dictionary<Guid, decimal>();

        var salaries = await _context.RecurrentEntries
            .Find(x => walletIdList.Contains(x.WalletId) && x.IsActive == true && x.Kind == EntryKind.Entrada)
            .ToListAsync();

        return salaries
            .GroupBy(x => x.WalletId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Value));
    }
}
