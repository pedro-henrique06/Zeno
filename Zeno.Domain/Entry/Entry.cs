using Zeno.Domain.Enum;

namespace Zeno.Domain.Entry;

public class Entry
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public decimal Value { get; set; }

    public EntryKind Kind { get; set; }

    public string Description { get; set; } = string.Empty;

    public Guid? TagId { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public bool IsRecurring { get; set; }

    public Entry() { }
}
