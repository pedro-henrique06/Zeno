namespace Zeno.Application.Responses.Summary;

public sealed class MovementsResponse
{
    public decimal Entrada { get; set; }
    public decimal Saida { get; set; }
    public decimal Diario { get; set; }
    public decimal Economia { get; set; }
    public decimal Cartao { get; set; }
}

public sealed class SummaryResponse
{
    public decimal Performance { get; set; }
    public decimal EconomizedPercent { get; set; }
    public decimal CostOfLiving { get; set; }
    public decimal DailyAverageReal { get; set; }
    public decimal DailyBudget { get; set; }
    public int DaysElapsed { get; set; }
    public int DaysRemaining { get; set; }
    public int DaysInMonth { get; set; }
    public MovementsResponse Movements { get; set; } = new();
}
