namespace Zeno.Application.Requests.Goals;

public class SaveGoalRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal MonthlyContribution { get; set; }
    public decimal InitialAmount { get; set; }
    public double AnnualRatePercent { get; set; }
}
