using System.Text.Json;

namespace Zeno.Application.Requests.Capture;

/// <summary>
/// Lançamento enviado por uma automação (Atalhos do iPhone). Os campos são tolerantes porque o
/// Atalhos pode mandar o valor como número ou como texto ("R$ 25,90").
/// </summary>
public sealed class CaptureEntryRequest
{
    /// <summary>Nome do estabelecimento ou descrição. Vazio vira "Apple Pay".</summary>
    public string? Title { get; set; }

    /// <summary>Valor como número ou texto (aceita "R$ 1.234,56", "1,234.56", "25.9"...).</summary>
    public JsonElement? Amount { get; set; }

    /// <summary>diario (padrão), entrada, saida, economia ou cartao.</summary>
    public string? Kind { get; set; }

    /// <summary>Nome do cartão usado (como na Carteira, ex.: "Nubank Mastercard"). Opcional.</summary>
    public string? Card { get; set; }

    /// <summary>Categoria do estabelecimento informada pelo Apple Pay. Opcional.</summary>
    public string? Category { get; set; }
}

public sealed class AddCaptureRuleRequest
{
    /// <summary>Texto procurado no comerciante ou na categoria (sem diferenciar maiúsculas nem acentos).</summary>
    public string Match { get; set; } = string.Empty;

    public Guid TagId { get; set; }
}
