using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Zeno.Application.Interfaces;

namespace Zeno.Infrastructure.SQL.Serialization;

/// <summary>
/// Serializes string fields in MongoDB using AES encryption.
/// Encrypted values are stored with an "ENC:" prefix for backward compatibility:
/// strings without the prefix are treated as legacy plain-text data and returned as-is.
/// Null BSON values are preserved as null (for nullable string fields).
/// </summary>
public class EncryptedStringSerializer : SerializerBase<string?>
{
    private const string Prefix = "ENC:";
    private readonly IEncryptionService _encryptionService;

    public EncryptedStringSerializer(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public override string? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;

        if (reader.CurrentBsonType == BsonType.Null)
        {
            reader.ReadNull();
            return null;
        }

        var raw = reader.ReadString();

        if (string.IsNullOrEmpty(raw))
            return raw;

        if (raw.StartsWith(Prefix, StringComparison.Ordinal))
        {
            try
            {
                return _encryptionService.Decrypt(raw[Prefix.Length..]);
            }
            catch
            {
                // Fallback: return raw value if decryption fails unexpectedly
                return raw;
            }
        }

        // Legacy unencrypted data — return as-is for backward compatibility
        return raw;
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string? value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
            return;
        }

        if (string.IsNullOrEmpty(value))
        {
            context.Writer.WriteString(value);
            return;
        }

        context.Writer.WriteString(Prefix + _encryptionService.Encrypt(value));
    }
}
