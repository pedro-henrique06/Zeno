namespace Zeno.Domain.Wallet;

public class Wallet
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "BRL";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
