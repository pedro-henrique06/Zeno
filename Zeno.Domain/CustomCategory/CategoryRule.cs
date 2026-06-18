namespace Zeno.Domain.CustomCategory;

public class CategoryRule
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}