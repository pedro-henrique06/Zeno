namespace Zeno.Domain.Goals;

/// <summary>Meta de economia do usuário (uma por usuário).</summary>
public class Goal
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Valor que o usuário quer alcançar.</summary>
    public decimal TargetAmount { get; set; }

    /// <summary>Aporte mensal planejado.</summary>
    public decimal MonthlyContribution { get; set; }

    /// <summary>Quanto o usuário já tinha guardado quando criou/atualizou a meta.</summary>
    public decimal InitialAmount { get; set; }

    /// <summary>Taxa de juros anual em porcentagem (ex.: 14.55).</summary>
    public double AnnualRatePercent { get; set; }

    /// <summary>Lançamentos de Economia a partir desta data contam como progresso.</summary>
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
