using Zeno.Domain.Enum;

namespace Zeno.Application.Requests;

public class CreateRecurringEntryRequest
{
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public EntryType Type { get; set; }
    public EntryKind Kind { get; set; }
    public Category Category { get; set; }
    public int DayOfMonth { get; set; }
    public Guid WalletId { get; set; }
}

public class UpdateRecurringEntryRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public EntryType Type { get; set; }
    public EntryKind Kind { get; set; }
    public Category Category { get; set; }
    public int DayOfMonth { get; set; }
    public bool IsActive { get; set; }
}

public class CreateFinancialGoalRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateOnly TargetDate { get; set; }
}

public class UpdateFinancialGoalRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateOnly TargetDate { get; set; }
}

public class CreateDebtRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal MonthlyPayment { get; set; }
}

public class UpdateDebtRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal MonthlyPayment { get; set; }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public int Type { get; set; }
}

public class UpdateCategoryRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Type { get; set; }
}

public class CreateCategoryRuleRequest
{
    public string Keyword { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
}

public class UpdateCategoryRuleRequest
{
    public Guid Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
}