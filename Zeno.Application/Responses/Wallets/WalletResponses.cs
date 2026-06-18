namespace Zeno.Application.Responses.Wallets;

public sealed class WalletResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "BRL";
    public DateTime CreatedAt { get; set; }
}