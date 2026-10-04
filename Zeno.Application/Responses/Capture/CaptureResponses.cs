using Zeno.Domain.Enum;

namespace Zeno.Application.Responses.Capture;

public sealed class CaptureKeyStatusResponse
{
    public bool Enabled { get; set; }
    public DateTime? CreatedAt { get; set; }
}

/// <summary>Resposta de criação: a chave em texto só é devolvida aqui, uma vez.</summary>
public sealed class CaptureKeyResponse
{
    public string Key { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class CaptureEntryResponse
{
    /// <summary>true quando o lançamento foi criado; false quando foi reenvio ignorado.</summary>
    public bool Created { get; set; }

    public bool Duplicate { get; set; }
    public Guid? EntryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public EntryKind Kind { get; set; }
    public DateTime Date { get; set; }
}
