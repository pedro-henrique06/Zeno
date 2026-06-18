namespace Zeno.Application.Responses;

public class GoalSimulationResponse
{
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int MonthsRemaining { get; set; }
    public decimal RequiredMonthlySaving { get; set; }
    public string AlertMessage { get; set; } = string.Empty;
}

public class PayoffSimulationResponse
{
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal MonthlyPayment { get; set; }
    public int EstimatedMonthsToPayOff { get; set; }
    public string AlertMessage { get; set; } = string.Empty;
}

public class DebtSummaryResponse
{
    public decimal TotalDebt { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
    public decimal AverageMonthlyPayment { get; set; }
    public int EstimatedMonthsToBecomeDebtFree { get; set; }
}