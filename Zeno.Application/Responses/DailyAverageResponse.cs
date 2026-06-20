namespace Zeno.Application.Responses;

public class DailyAverageResponse
{
    public Guid WalletId { get; set; }
    public int Months { get; set; }
    public decimal AverageDailyIncome { get; set; }
    public decimal AverageDailyExpense { get; set; }
    public decimal AverageDailyNet { get; set; }
}
