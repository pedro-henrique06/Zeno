using Zeno.Application.Responses.Entries;

namespace Zeno.Application.Interfaces;

public interface IDashboardService
{
    Task<MonthlySummaryResponse> GetMonthlySummaryAsync(Guid userId, int month, int year);
    Task<IEnumerable<CategorySummaryResponse>> GetCategorySummaryAsync(Guid userId, int month, int year);
}