using Zeno.Application.Requests;
using Zeno.Application.Responses;
using Zeno.Domain.Wallet;

namespace Zeno.Application.Interfaces;

public interface IBalanceService
{
    Task<DailyBalancesResponse> GetDailyBalancesAsync(Guid userId, Guid walletId, int month, int year);
    Task<DailyBalancesResponse> GetAggregatedDailyBalancesAsync(Guid userId, int month, int year);
    Task<DailyAverageResponse> GetDailyAverageAsync(Guid userId, Guid walletId, int months);
    Task<ForecastResponse> GetForecastAsync(Guid userId, Guid walletId, int months);
    Task<CardInvoiceResponse> GetCardInvoiceAsync(Guid userId, Guid walletId, int month, int year);
    Task<DailyForecastResponse> GetDailyForecastAsync(Guid userId, Guid walletId, int month, int year);
    Task<Wallet> UpdateBudgetAsync(Guid userId, Guid walletId, UpdateWalletBudgetRequest request);
}
