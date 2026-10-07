using MongoDB.Bson;
using Zeno.Application.Services;

namespace Zeno.Infrastructure.SQL.Serialization;

public enum EncryptedFieldKind
{
    /// <summary>String gravada como "ENC:" + valor.</summary>
    String,

    /// <summary>Decimal gravado como string criptografada.</summary>
    Decimal,

    /// <summary>Data opcional gravada como string criptografada.</summary>
    NullableDateTime
}

public sealed record EncryptedField(string Name, EncryptedFieldKind Kind);

/// <summary>
/// Quais campos de cada coleção são criptografados e como reconhecer um documento que ainda tem algum
/// campo em texto puro ou no formato antigo (AES-CBC). Os nomes são os do BSON (PascalCase, como as
/// propriedades das entidades). Mantenha esta lista igual aos class maps do ZenoMongoContext.
/// </summary>
public static class EncryptionMigrationRules
{
    private const string StringPrefix = "ENC:";

    public static readonly IReadOnlyDictionary<string, EncryptedField[]> Collections =
        new Dictionary<string, EncryptedField[]>
        {
            ["users"] =
            [
                new("Name", EncryptedFieldKind.String),
                new("Phone", EncryptedFieldKind.String),
                new("Document", EncryptedFieldKind.String),
                new("DailyBudget", EncryptedFieldKind.Decimal),
                new("BirthDate", EncryptedFieldKind.NullableDateTime),
            ],
            ["entries"] =
            [
                new("Title", EncryptedFieldKind.String),
                new("Description", EncryptedFieldKind.String),
                new("Value", EncryptedFieldKind.Decimal),
            ],
            ["houses"] =
            [
                new("Name", EncryptedFieldKind.String),
                new("Description", EncryptedFieldKind.String),
                new("GoalName", EncryptedFieldKind.String),
                new("GoalTargetAmount", EncryptedFieldKind.Decimal),
            ],
            ["goals"] =
            [
                new("Name", EncryptedFieldKind.String),
                new("TargetAmount", EncryptedFieldKind.Decimal),
                new("MonthlyContribution", EncryptedFieldKind.Decimal),
                new("InitialAmount", EncryptedFieldKind.Decimal),
            ],
            ["tags"] =
            [
                new("Name", EncryptedFieldKind.String),
            ],
            ["monthlyexpensecategories"] =
            [
                new("Name", EncryptedFieldKind.String),
                new("Amount", EncryptedFieldKind.Decimal),
            ],
            ["capturerules"] =
            [
                new("Match", EncryptedFieldKind.String),
            ],
            ["pushsubscriptions"] =
            [
                new("P256dh", EncryptedFieldKind.String),
                new("Auth", EncryptedFieldKind.String),
            ],
        };

    /// <summary>Campos criptografados dos moradores, guardados dentro do array Members da casa.</summary>
    public static readonly EncryptedField[] HouseMemberFields =
    [
        new("Name", EncryptedFieldKind.String),
        new("Email", EncryptedFieldKind.String),
    ];

    /// <summary>true se algum campo criptografado do documento ainda está em texto puro ou no formato antigo.</summary>
    public static bool NeedsMigration(string collection, BsonDocument document)
    {
        if (!Collections.TryGetValue(collection, out var fields))
            return false;

        if (fields.Any(f => FieldNeedsMigration(f, document)))
            return true;

        return collection == "houses" && MembersOf(document).Any(m => HouseMemberFields.Any(f => FieldNeedsMigration(f, m)));
    }

    /// <summary>
    /// true se algum valor já criptografado não pôde ser decifrado (chave errada ou dado adulterado). Esses
    /// documentos nunca devem ser regravados pela migração, para não embrulhar lixo em outra camada.
    /// </summary>
    public static bool HasUnreadableValue(string collection, BsonDocument document, Func<string, string> decrypt)
    {
        if (!Collections.TryGetValue(collection, out var fields))
            return false;

        var unreadable = fields.Any(f => !IsReadable(f, document, decrypt));
        if (unreadable)
            return true;

        return collection == "houses" && MembersOf(document).Any(m => HouseMemberFields.Any(f => !IsReadable(f, m, decrypt)));
    }

    /// <summary>true se o refresh token ainda está em texto puro (o valor atual é o hash SHA-256).</summary>
    public static bool RefreshTokenNeedsHash(BsonDocument document)
    {
        return document.TryGetValue("Token", out var token)
               && token.IsString
               && !TokenHasher.IsHash(token.AsString);
    }

    private static bool FieldNeedsMigration(EncryptedField field, BsonDocument document)
    {
        if (!document.TryGetValue(field.Name, out var value) || value.IsBsonNull)
            return false;

        switch (field.Kind)
        {
            case EncryptedFieldKind.String:
                return value.IsString
                       && value.AsString.Length > 0
                       && !IsCurrentString(value.AsString);

            case EncryptedFieldKind.Decimal:
                return !(value.IsString && AesEncryptionService.IsCurrentFormat(value.AsString));

            case EncryptedFieldKind.NullableDateTime:
                if (value.IsString)
                    return value.AsString.Length > 0 && !IsCurrentString(value.AsString);
                return value.IsBsonDateTime;

            default:
                return false;
        }
    }

    private static bool IsReadable(EncryptedField field, BsonDocument document, Func<string, string> decrypt)
    {
        if (!document.TryGetValue(field.Name, out var value) || !value.IsString)
            return true;

        var raw = value.AsString;
        string? cipher = field.Kind switch
        {
            EncryptedFieldKind.Decimal => raw,
            _ => raw.StartsWith(StringPrefix, StringComparison.Ordinal) ? raw[StringPrefix.Length..] : null,
        };

        if (string.IsNullOrEmpty(cipher))
            return true;

        try
        {
            decrypt(cipher);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsCurrentString(string raw)
    {
        return raw.StartsWith(StringPrefix, StringComparison.Ordinal)
               && AesEncryptionService.IsCurrentFormat(raw[StringPrefix.Length..]);
    }

    private static IEnumerable<BsonDocument> MembersOf(BsonDocument house)
    {
        return house.TryGetValue("Members", out var members) && members.IsBsonArray
            ? members.AsBsonArray.Where(m => m.IsBsonDocument).Select(m => m.AsBsonDocument)
            : Enumerable.Empty<BsonDocument>();
    }
}
