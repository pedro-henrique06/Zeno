using Zeno.Application.Responses.Balances;

namespace Zeno.Application.Interfaces;

public interface IBalanceService
{
    Task<BalancesResponse> GetMonthlyBalances(Guid userId, int month, int year);
    Task<BalancesHorizonResponse> GetYearlyBalances(Guid userId, int year);
}
