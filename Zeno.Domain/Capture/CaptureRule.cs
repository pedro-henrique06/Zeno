namespace Zeno.Domain.Capture;

/// <summary>
/// Regra de categorização dos gastos capturados: se o comerciante ou a categoria do Apple Pay
/// contiver <see cref="Match"/> (sem diferenciar maiúsculas nem acentos), o lançamento recebe a tag.
/// </summary>
public class CaptureRule
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Match { get; set; } = string.Empty;

    public Guid TagId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
