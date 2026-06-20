namespace Zeno.Application.Responses;

public class DailyForecastResponse
{
    public Guid WalletId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal? DailyBudget { get; set; }
    public decimal SpentSoFar { get; set; }
    public int RemainingDays { get; set; }
    public decimal RecommendedDailySpend { get; set; }
    public bool IsOverBudget { get; set; }
}
