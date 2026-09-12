using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Zeno.Application.Interfaces;

namespace Zeno.Infrastructure.SQL.Serialization;

public class EncryptedDecimalSerializer : SerializerBase<decimal>
{
    private readonly IEncryptionService _encryptionService;

    public EncryptedDecimalSerializer(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public override decimal Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        return reader.CurrentBsonType switch
        {
            BsonType.String => _encryptionService.DecryptDecimal(reader.ReadString()),
            BsonType.Decimal128 => (decimal)reader.ReadDecimal128(),
            BsonType.Double => (decimal)reader.ReadDouble(),
            BsonType.Int32 => reader.ReadInt32(),
            BsonType.Int64 => reader.ReadInt64(),
            var type => throw new BsonSerializationException($"Não é possível desserializar BsonType {type} como decimal criptografado."),
        };
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, decimal value)
    {
        context.Writer.WriteString(_encryptionService.EncryptDecimal(value));
    }
}
