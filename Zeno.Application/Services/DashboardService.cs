using Zeno.Application.Interfaces;
using Zeno.Application.Responses.Entries;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IEntryRepository _entryRepository;
    private readonly IWalletRepository _walletRepository;

    public DashboardService(IEntryRepository entryRepository, IWalletRepository walletRepository)
    {
        _entryRepository = entryRepository;
        _walletRepository = walletRepository;
    }

    public async Task<MonthlySummaryResponse> GetMonthlySummaryAsync(Guid userId, int month, int year)
    {
        var wallets = await _walletRepository.GetAllByUserAsync(userId);
        var walletList = wallets.ToList();

        if (walletList.Count == 0)
        {
            return new MonthlySummaryResponse
            {
                TotalIncome = 0,
                TotalExpenses = 0,
                CurrentBalance = 0,
                NeedsLimit = 0,
                WantsLimit = 0,
                SavingsLimit = 0,
                BiggestExpenseCategory = string.Empty,
                IsOverNeedsBudget = false
            };
        }

        var totalIncome = 0m;
        var totalExpenses = 0m;
        decimal currentBalance = 0;
        var categoryTotals = new Dictionary<int, decimal>();

        foreach (var wallet in walletList)
        {
            currentBalance += wallet.Balance;
            Guid walletId = wallet.Id;

            var income = await _entryRepository.GetTotalByTypeAndWalletAsync(month, year, walletId, (int)EntryType.Credit);
            var expenses = await _entryRepository.GetTotalByTypeAndWalletAsync(month, year, walletId, (int)EntryType.Debit);

            totalIncome += income;
            totalExpenses += expenses;

            var categories = await _entryRepository.GetCategoryTotalsAsync(month, year, walletId);
            foreach (var catData in categories)
            {
                if (categoryTotals.ContainsKey(catData.Category))
                    categoryTotals[catData.Category] += catData.Total;
                else
                    categoryTotals[catData.Category] = catData.Total;
            }
        }

        var needsLimit = totalIncome * 0.50m;
        var wantsLimit = totalIncome * 0.30m;
        var savingsLimit = totalIncome * 0.20m;
        var biggestCategory = categoryTotals.OrderByDescending(x => x.Value).FirstOrDefault();
        var biggestCategoryName = biggestCategory.Key > 0 ? ((Category)biggestCategory.Key).ToString() : string.Empty;

        return new MonthlySummaryResponse
        {
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            CurrentBalance = currentBalance,
            NeedsLimit = needsLimit,
            WantsLimit = wantsLimit,
            SavingsLimit = savingsLimit,
            BiggestExpenseCategory = biggestCategoryName,
            IsOverNeedsBudget = totalExpenses > needsLimit
        };
    }

    public async Task<IEnumerable<CategorySummaryResponse>> GetCategorySummaryAsync(Guid userId, int month, int year)
    {
        var wallets = await _walletRepository.GetAllByUserAsync(userId);
        var walletList = wallets.ToList();

        var categoryTotals = new Dictionary<int, decimal>();
        var totalExpenses = 0m;

        foreach (var wallet in walletList)
        {
            Guid walletId = wallet.Id;
            var categories = await _entryRepository.GetCategoryTotalsAsync(month, year, walletId);
            foreach (var catData in categories)
            {
                if (categoryTotals.ContainsKey(catData.Category))
                    categoryTotals[catData.Category] += catData.Total;
                else
                    categoryTotals[catData.Category] = catData.Total;
                totalExpenses += catData.Total;
            }
        }

        if (totalExpenses == 0)
            return Enumerable.Empty<CategorySummaryResponse>();

        return categoryTotals
            .OrderByDescending(x => x.Value)
            .Select(x => new CategorySummaryResponse
            {
                Category = ((Category)x.Key).ToString(),
                Total = x.Value,
                Percentage = Math.Round((x.Value / totalExpenses) * 100, 2)
            });
    }
}