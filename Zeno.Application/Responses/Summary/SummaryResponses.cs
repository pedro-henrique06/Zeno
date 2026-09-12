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

public sealed class EconomizedMonthResponse
{
    public int Month { get; set; }
    public decimal EconomizedPercent { get; set; }
    public decimal Economia { get; set; }
    public decimal Entrada { get; set; }
}

public sealed class EconomizedHorizonResponse
{
    public int Year { get; set; }
    public decimal EconomizedPercent { get; set; }
    public decimal Economia { get; set; }
    public decimal Entrada { get; set; }
    public List<EconomizedMonthResponse> Months { get; set; } = new();
}

public sealed class PerformanceMonthResponse
{
    public int Month { get; set; }
    public decimal Performance { get; set; }
}

public sealed class PerformanceHorizonResponse
{
    public int Year { get; set; }
    public List<PerformanceMonthResponse> Months { get; set; } = new();
}

public sealed class CostOfLivingMonthResponse
{
    public int Month { get; set; }
    public decimal CostOfLiving { get; set; }
}

public sealed class CostOfLivingHorizonResponse
{
    public int Year { get; set; }
    public decimal CostOfLiving { get; set; }
    public List<CostOfLivingMonthResponse> Months { get; set; } = new();
}

public sealed class DailyAverageMonthResponse
{
    public int Month { get; set; }
    public decimal DailyAverage { get; set; }
    public decimal TotalDiario { get; set; }
    public int DaysInMonth { get; set; }
}

public sealed class DailyAverageHorizonResponse
{
    public int Year { get; set; }
    public List<DailyAverageMonthResponse> Months { get; set; } = new();
}
