namespace Zeno.Application.Requests;

public class ProjectionSimulationRequest
{
    public Guid? WalletId { get; set; }
    public decimal ExtraExpenseAmount { get; set; }
    public bool IsRecurring { get; set; } = true;
    public int MonthsToProject { get; set; } = 6;
}
