using Zeno.Domain.Enum;

namespace Zeno.Application.Responses.Entries;

public sealed class EntryResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public Guid WalletId { get; set; }
}

public sealed class MonthlySummaryResponse
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal NeedsLimit { get; set; }
    public decimal WantsLimit { get; set; }
    public decimal SavingsLimit { get; set; }
    public string BiggestExpenseCategory { get; set; } = string.Empty;
    public bool IsOverNeedsBudget { get; set; }
}

public sealed class CategorySummaryResponse
{
    public string Category { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal Percentage { get; set; }
}