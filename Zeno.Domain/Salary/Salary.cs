namespace Zeno.Domain.Salary;

public class Salary
{
    public Guid? Id { get; set; }
    public Guid UserId { get; set; }
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int DayOfMonth { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastProcessedAt { get; set; }
}
