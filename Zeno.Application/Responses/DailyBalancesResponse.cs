namespace Zeno.Application.Responses;

public class DailyBalancesResponse
{
    public Guid? WalletId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public List<DailyBalanceEntry> Days { get; set; } = new();
}
