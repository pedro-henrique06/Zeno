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
    public Guid? TagId { get; set; }
    public string? Description { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public EntryKind Kind { get; set; }
    public DateTime Date { get; set; }

    /// <summary>Card the purchase was made with, when the Shortcut sent one.</summary>
    public Guid? CardId { get; set; }

    /// <summary>False while that card is not yet marked as credit or debit.</summary>
    public bool? CardClassified { get; set; }
}

public sealed class CaptureRuleResponse
{
    public Guid Id { get; set; }
    public string Match { get; set; } = string.Empty;
    public Guid TagId { get; set; }
}

public sealed class CaptureCardResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>"credit", "debit", or null when not classified yet.</summary>
    public string? Type { get; set; }

    public int PurchaseCount { get; set; }
    public DateTime LastSeenAt { get; set; }
}

public sealed class ClassifyCaptureCardResponse
{
    public CaptureCardResponse Card { get; set; } = new();

    /// <summary>How many earlier purchases of this card changed kind (0 unless applyToPast).</summary>
    public long UpdatedEntries { get; set; }
}
