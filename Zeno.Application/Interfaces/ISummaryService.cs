using Zeno.Application.Responses.Summary;

namespace Zeno.Application.Interfaces;

public interface ISummaryService
{
    Task<SummaryResponse> GetMonthlySummary(Guid userId, int month, int year);
    Task<EconomizedHorizonResponse> GetEconomizedHorizon(Guid userId, int year);
    Task<PerformanceHorizonResponse> GetPerformanceHorizon(Guid userId, int year);
    Task<CostOfLivingHorizonResponse> GetCostOfLivingHorizon(Guid userId, int year);
    Task<DailyAverageHorizonResponse> GetDailyAverageHorizon(Guid userId, int year);
}
