namespace Zeno.Domain.Capture;

/// <summary>
/// Chave pessoal e revogável que permite apenas CRIAR lançamentos (usada pelas automações do
/// app Atalhos do iPhone, p. ex. a cada pagamento no Apple Pay). Só o hash é guardado.
/// </summary>
public class CaptureKey
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string KeyHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Impressão digital do último lançamento capturado, para ignorar reenvios imediatos.</summary>
    public string? LastFingerprint { get; set; }

    public DateTime? LastCaptureAt { get; set; }
}
