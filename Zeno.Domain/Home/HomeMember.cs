using Zeno.Domain.Enum;

namespace Zeno.Domain.Home;

public class HomeMember
{
    public Guid HomeId { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public HomeRole Role { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
