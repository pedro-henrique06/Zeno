namespace Zeno.Application.Responses;

public class ForecastResponse
{
    public Guid WalletId { get; set; }
    public int Months { get; set; }
    public decimal CurrentBalance { get; set; }
    public List<DailyBalanceEntry> Days { get; set; } = new();
}
