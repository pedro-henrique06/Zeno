namespace Zeno.Application.Responses;

public class ProjectionResponse
{
    public Guid WalletId { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal AverageMonthlyIncome { get; set; }
    public decimal AverageMonthlyExpenses { get; set; }
    public decimal ExtraExpenseAmount { get; set; }
    public bool IsRecurring { get; set; }
    public List<ProjectionMonthResult> Months { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}
