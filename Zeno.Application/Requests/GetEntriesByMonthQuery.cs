namespace Zeno.Application.Requests;

public class GetEntriesByMonthQuery
{
    public int? Month { get; set; }
    public int? Year { get; set; }
    public Guid? WalletId { get; set; }
}
