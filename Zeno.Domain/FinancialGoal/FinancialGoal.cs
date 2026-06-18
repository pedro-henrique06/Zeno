namespace Zeno.Domain.FinancialGoal;

public class FinancialGoal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateOnly TargetDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}