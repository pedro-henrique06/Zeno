using Zeno.Domain.Enum;

namespace Zeno.Application.Responses.Houses;

public sealed class HouseGoalResponse
{
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public DateTime StartDate { get; set; }

    /// <summary>Soma da fatia de 20% de cada mês desde o início da meta (até 36 meses).</summary>
    public decimal AccumulatedAmount { get; set; }

    public decimal ProgressPercent { get; set; }

    /// <summary>Fatia de 20% do mês consultado.</summary>
    public decimal MonthlyContribution { get; set; }

    /// <summary>Meses restantes mantendo o aporte do mês consultado; null se o aporte for zero.</summary>
    public int? MonthsRemaining { get; set; }
}

/// <summary>
/// Orçamento 50/30/20 da casa. Traz apenas totais: nunca devolve renda, saldo ou lançamentos
/// individuais dos moradores.
/// </summary>
public sealed class HouseBudgetResponse
{
    public Guid HouseId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    /// <summary>Moeda do dono da casa; os valores dos moradores são somados sem conversão.</summary>
    public Currency Currency { get; set; }

    public int ResidentCount { get; set; }
    public bool IsOwner { get; set; }

    /// <summary>Soma das entradas do mês de todos os moradores.</summary>
    public decimal TotalIncome { get; set; }

    /// <summary>50%: necessidades.</summary>
    public decimal Needs { get; set; }

    /// <summary>30%: desejos (saldo livre).</summary>
    public decimal Wants { get; set; }

    /// <summary>20%: meta.</summary>
    public decimal Savings { get; set; }

    /// <summary>Wants dividido igualmente entre os moradores.</summary>
    public decimal FreePerPerson { get; set; }

    public HouseGoalResponse? Goal { get; set; }
}
