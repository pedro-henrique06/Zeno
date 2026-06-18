namespace Zeno.Domain.Debt;

public class Debt
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal MonthlyPayment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public decimal RemainingAmount => TotalAmount - PaidAmount;
}