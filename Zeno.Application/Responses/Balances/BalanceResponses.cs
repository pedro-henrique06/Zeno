namespace Zeno.Application.Responses.Balances;

public sealed class BalanceDayResponse
{
    public int Day { get; set; }
    public decimal Entrada { get; set; }
    public decimal Saida { get; set; }
    public decimal Diario { get; set; }
    public decimal Economia { get; set; }
    public decimal Cartao { get; set; }
    public decimal Balance { get; set; }
    public bool IsProjected { get; set; }
    public bool IsToday { get; set; }
}

public sealed class BalancesResponse
{
    public int Month { get; set; }
    public int Year { get; set; }
    public List<BalanceDayResponse> Days { get; set; } = new();
}

public sealed class BalancesHorizonResponse
{
    public int Year { get; set; }
    public List<BalancesResponse> Months { get; set; } = new();
}
