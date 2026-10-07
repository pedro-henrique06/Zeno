using System.Globalization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Zeno.Application.Interfaces;

namespace Zeno.Infrastructure.SQL.Serialization;

/// <summary>
/// Grava um DateTime? criptografado (ISO 8601 em UTC dentro do envelope "ENC:"). Valores antigos, gravados
/// como data BSON, continuam legíveis; qualquer valor gravado passa a sair criptografado.
/// </summary>
public class EncryptedNullableDateTimeSerializer : SerializerBase<DateTime?>
{
    private const string Prefix = "ENC:";
    private readonly IEncryptionService _encryptionService;

    public EncryptedNullableDateTimeSerializer(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public override DateTime? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;

        switch (reader.CurrentBsonType)
        {
            case BsonType.Null:
                reader.ReadNull();
                return null;

            case BsonType.DateTime:
                return BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(reader.ReadDateTime());

            case BsonType.String:
                var raw = reader.ReadString();
                if (string.IsNullOrEmpty(raw))
                    return null;

                var plain = raw.StartsWith(Prefix, StringComparison.Ordinal)
                    ? _encryptionService.Decrypt(raw[Prefix.Length..])
                    : raw;
                return DateTime.Parse(plain, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

            case var type:
                throw new BsonSerializationException($"Não é possível desserializar BsonType {type} como data criptografada.");
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTime? value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
            return;
        }

        var iso = value.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        context.Writer.WriteString(Prefix + _encryptionService.Encrypt(iso));
    }
}
