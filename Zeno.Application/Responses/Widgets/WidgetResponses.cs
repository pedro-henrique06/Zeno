namespace Zeno.Application.Responses.Widgets;

public sealed class WidgetKeyStatusResponse
{
    public bool Enabled { get; set; }
    public DateTime? CreatedAt { get; set; }
}

/// <summary>Resposta de criação: a chave em texto só é devolvida aqui, uma vez.</summary>
public sealed class WidgetKeyResponse
{
    public string Key { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class WidgetGoalResponse
{
    public string Name { get; set; } = string.Empty;
    public decimal ProgressPercent { get; set; }
    public decimal SavedAmount { get; set; }
    public decimal TargetAmount { get; set; }
}

/// <summary>Resumo compacto para o widget da tela inicial.</summary>
public sealed class WidgetSummaryResponse
{
    public string Currency { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }

    /// <summary>Saldo do mês (mesmo valor da performance no app).</summary>
    public decimal MonthBalance { get; set; }
    public decimal Income { get; set; }

    /// <summary>Saídas + diário + cartão do mês.</summary>
    public decimal Expenses { get; set; }

    public decimal DailyBudget { get; set; }
    public decimal SpentToday { get; set; }

    /// <summary>DailyBudget - SpentToday; negativo quando passou do limite.</summary>
    public decimal AvailableToday { get; set; }

    public WidgetGoalResponse? Goal { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
}
