namespace Zeno.Application.Requests.Wallets;

public sealed class CreateWalletRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Currency { get; set; } = "BRL";
}

public sealed class UpdateWalletRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}