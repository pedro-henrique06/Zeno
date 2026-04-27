namespace Zeno.Application.Responses;

public class BudgetAlertResponse
{
    public Guid HomeId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal MaxNeedsLimit { get; set; }
    public decimal NeedsUsagePercentage { get; set; }
    public decimal WantsLimit { get; set; }
    public decimal SavingsLimit { get; set; }
    public bool IsOverBudget { get; set; }
    public string AlertMessage { get; set; } = string.Empty;
}
