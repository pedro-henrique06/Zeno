using Zeno.Domain.Enum;

namespace Zeno.Domain.Capture;

/// <summary>
/// A card seen in Apple Pay captures (the Wallet card name the Shortcut sends). The user classifies it
/// as credit (purchases become <see cref="EntryKind.Cartao"/>) or debit (<see cref="EntryKind.Diario"/>).
/// </summary>
public class CaptureCard
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Display name as it last came from the Wallet (encrypted at rest).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Hash of the normalized name, so the card can be found without decrypting every name.</summary>
    public string NameKey { get; set; } = string.Empty;

    /// <summary>Kind for this card's purchases; null until the user classifies it.</summary>
    public EntryKind? Kind { get; set; }

    public int PurchaseCount { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}
