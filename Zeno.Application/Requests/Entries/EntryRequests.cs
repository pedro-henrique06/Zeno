using Zeno.Domain.Enum;

namespace Zeno.Application.Requests.Entries;

public sealed class CreateEntryRequest
{
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public EntryKind Kind { get; set; }
    public string? Description { get; set; }
    public Guid? TagId { get; set; }
    public DateTime Date { get; set; }
}

public sealed class UpdateEntryRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public EntryKind Kind { get; set; }
    public string? Description { get; set; }
    public Guid? TagId { get; set; }
    public DateTime Date { get; set; }
}

public sealed class DeleteEntryRequest
{
    public Guid Id { get; set; }
}
