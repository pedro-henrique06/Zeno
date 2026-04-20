using Zeno.Domain.Enum;

namespace Zeno.Domain.Home;

public class HomeExpense
{
    public Guid? Id { get; set; }
    public Guid HomeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public Category Category { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
