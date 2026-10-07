using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Zeno.Application.Services;
using Zeno.Infrastructure.SQL.Serialization;

namespace Zeno.Tests;

public class EncryptedSerializerTests
{
    private const string Key = "test-key-with-enough-entropy-1234567890";

    private sealed class SecretBag
    {
        public string? Text { get; set; }
        public decimal Amount { get; set; }
        public decimal? MaybeAmount { get; set; }
        public DateTime? Birth { get; set; }
    }

    static EncryptedSerializerTests()
    {
        var enc = new AesEncryptionService(Key);
        var encryptedDecimal = new EncryptedDecimalSerializer(enc);

        if (!BsonClassMap.IsClassMapRegistered(typeof(SecretBag)))
        {
            BsonClassMap.RegisterClassMap<SecretBag>(cm =>
            {
                cm.AutoMap();
                cm.GetMemberMap(x => x.Text).SetSerializer(new EncryptedStringSerializer(enc));
                cm.GetMemberMap(x => x.Amount).SetSerializer(encryptedDecimal);
                cm.GetMemberMap(x => x.MaybeAmount).SetSerializer(new NullableSerializer<decimal>(encryptedDecimal));
                cm.GetMemberMap(x => x.Birth).SetSerializer(new EncryptedNullableDateTimeSerializer(enc));
            });
        }
    }

    [Fact]
    public void Writes_EverythingEncrypted_AndNothingReadable()
    {
        var bag = new SecretBag
        {
            Text = "Maria Aparecida",
            Amount = 1234.56m,
            MaybeAmount = 99m,
            Birth = new DateTime(1990, 5, 17, 0, 0, 0, DateTimeKind.Utc)
        };

        var doc = bag.ToBsonDocument();

        Assert.StartsWith("ENC:v2.", doc["Text"].AsString);
        Assert.StartsWith("v2.", doc["Amount"].AsString);
        Assert.StartsWith("v2.", doc["MaybeAmount"].AsString);
        Assert.StartsWith("ENC:v2.", doc["Birth"].AsString);
        Assert.DoesNotContain("Maria", doc.ToJson());
        Assert.DoesNotContain("1234", doc.ToJson());
        Assert.DoesNotContain("1990", doc.ToJson());
    }

    [Fact]
    public void RoundTrips_Values()
    {
        var bag = new SecretBag
        {
            Text = "Maria Aparecida",
            Amount = 1234.56m,
            MaybeAmount = 99m,
            Birth = new DateTime(1990, 5, 17, 0, 0, 0, DateTimeKind.Utc)
        };

        var back = BsonSerializer.Deserialize<SecretBag>(bag.ToBsonDocument());

        Assert.Equal("Maria Aparecida", back.Text);
        Assert.Equal(1234.56m, back.Amount);
        Assert.Equal(99m, back.MaybeAmount);
        Assert.Equal(bag.Birth, back.Birth);
    }

    [Fact]
    public void Nulls_StayNull()
    {
        var doc = new SecretBag { Text = null, Amount = 1m, MaybeAmount = null, Birth = null }.ToBsonDocument();

        Assert.True(doc["Text"].IsBsonNull);
        Assert.True(doc["MaybeAmount"].IsBsonNull);
        Assert.True(doc["Birth"].IsBsonNull);

        var back = BsonSerializer.Deserialize<SecretBag>(doc);
        Assert.Null(back.Text);
        Assert.Null(back.MaybeAmount);
        Assert.Null(back.Birth);
    }

    [Fact]
    public void Reads_LegacyPlainDocuments()
    {
        var birth = new DateTime(1985, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var doc = new BsonDocument
        {
            { "Text", "texto puro" },
            { "Amount", 12.5 },
            { "MaybeAmount", BsonNull.Value },
            { "Birth", new BsonDateTime(birth) },
        };

        var back = BsonSerializer.Deserialize<SecretBag>(doc);

        Assert.Equal("texto puro", back.Text);
        Assert.Equal(12.5m, back.Amount);
        Assert.Null(back.MaybeAmount);
        Assert.Equal(birth, back.Birth);
    }

    [Fact]
    public void Reads_LegacyCbcDocuments()
    {
        var doc = new BsonDocument
        {
            { "Text", "ENC:" + AesEncryptionServiceTests.LegacyEncrypt(Key, "antigo") },
            { "Amount", AesEncryptionServiceTests.LegacyEncrypt(Key, "7.25") },
            { "MaybeAmount", BsonNull.Value },
            { "Birth", BsonNull.Value },
        };

        var back = BsonSerializer.Deserialize<SecretBag>(doc);

        Assert.Equal("antigo", back.Text);
        Assert.Equal(7.25m, back.Amount);
    }
}
