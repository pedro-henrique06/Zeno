namespace Zeno.Application.Responses;

public class ProjectionMonthResult
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal ProjectedIncome { get; set; }
    public decimal ProjectedExpenses { get; set; }
    public decimal ProjectedBalance { get; set; }
    public bool IsOverBudget { get; set; }
}
