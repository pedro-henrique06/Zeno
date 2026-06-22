namespace Zeno.Application.Requests.MonthlyExpenseCategories;

public class CreateMonthlyExpenseCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class UpdateMonthlyExpenseCategoryRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
