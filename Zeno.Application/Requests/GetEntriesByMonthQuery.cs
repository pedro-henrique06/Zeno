using Zeno.Domain.Enum;

namespace Zeno.Application.Requests;

public class GetEntriesByMonthQuery
{
    public int? Month { get; set; }
    public int? Year { get; set; }
    public Guid? WalletId { get; set; }
    public EntryType? Type { get; set; }
    public Category? Category { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}