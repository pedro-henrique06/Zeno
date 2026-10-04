namespace Zeno.Application.Requests.Houses;

public sealed class SaveHouseGoalRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
}
