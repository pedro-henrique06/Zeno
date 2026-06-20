namespace Zeno.Application.Responses;

public class CardInvoiceResponse
{
    public Guid WalletId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Total { get; set; }
}
