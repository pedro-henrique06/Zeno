namespace Zeno.Domain.Wallet;

public class Account
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Bank { get; set; } = string.Empty;
    public string Type { get; set; } = "checking";
    public decimal Balance { get; set; }
    public Guid WalletId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class AccountTypes
{
    public const string Checking = "checking";
    public const string Savings = "savings";
    public const string Investment = "investment";
    public const string Credit = "credit";
    public const string Other = "other";

    public static readonly string[] All = { Checking, Savings, Investment, Credit, Other };
}