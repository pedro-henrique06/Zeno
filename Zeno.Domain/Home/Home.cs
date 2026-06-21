using Zeno.Domain.Enum;

namespace Zeno.Domain.Home;

public class Home
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SplitMode SplitMode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
