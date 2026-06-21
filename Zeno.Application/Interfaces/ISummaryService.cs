using Zeno.Application.Responses.Summary;

namespace Zeno.Application.Interfaces;

public interface ISummaryService
{
    Task<SummaryResponse> GetMonthlySummary(Guid userId, int month, int year);
}
