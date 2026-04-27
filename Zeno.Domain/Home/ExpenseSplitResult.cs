namespace Zeno.Domain.Home;

public class ExpenseSplitResult
{
    public Guid WalletId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string WalletName { get; set; } = string.Empty;
    public decimal WalletIncome { get; set; }
    public decimal SalaryAmount { get; set; }
    public decimal SalaryWeight { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalSalary { get; set; }
    public decimal Percentage { get; set; }
    public decimal AmountToPay { get; set; }
}
