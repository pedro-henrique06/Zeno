using Zeno.Domain.Enum;

namespace Zeno.Domain.Recurring;

public class RecurringEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid WalletId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public EntryType Type { get; set; }
    public EntryKind Kind { get; set; }
    public Category Category { get; set; }
    public int DayOfMonth { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastProcessedAt { get; set; }
}
