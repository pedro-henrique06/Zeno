namespace Zeno.Domain.Widgets;

/// <summary>
/// Chave pessoal e revogável usada pelo widget do iPhone (Scriptable) para ler um resumo
/// somente leitura. Só o hash SHA-256 é guardado; a chave em texto aparece uma única vez.
/// </summary>
public class WidgetKey
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string KeyHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
