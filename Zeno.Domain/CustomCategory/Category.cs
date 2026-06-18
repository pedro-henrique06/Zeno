namespace Zeno.Domain.CustomCategory;

public class Category
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static class CategoryType
    {
        public const int Income = 0;
        public const int Expense = 1;
    }
}