namespace Zeno.Domain.Home;

public class ExpenseSplitResult
{
    public Guid WalletId { get; set; }
    public string WalletName { get; set; } = string.Empty;
    public decimal WalletIncome { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal Percentage { get; set; }
    public decimal AmountToPay { get; set; }
}
