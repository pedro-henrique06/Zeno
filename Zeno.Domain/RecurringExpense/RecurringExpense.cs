namespace Zeno.Domain.RecurringExpense;

public class RecurringExpense
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid WalletId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int DayOfMonth { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastProcessedAt { get; set; }
}