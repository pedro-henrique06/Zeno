namespace Zeno.Application.Responses.Goals;

public class GoalResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal MonthlyContribution { get; set; }
    public decimal InitialAmount { get; set; }
    public double AnnualRatePercent { get; set; }
    public DateTime StartDate { get; set; }

    /// <summary>InitialAmount + lançamentos de Economia desde StartDate.</summary>
    public decimal SavedAmount { get; set; }

    /// <summary>SavedAmount / TargetAmount em %, limitado a 100.</summary>
    public decimal ProgressPercent { get; set; }
}
