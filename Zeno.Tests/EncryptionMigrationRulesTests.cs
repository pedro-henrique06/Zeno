using MongoDB.Bson;
using Zeno.Application.Services;
using Zeno.Infrastructure.SQL.Serialization;

namespace Zeno.Tests;

public class EncryptionMigrationRulesTests
{
    private const string Key = "test-key-with-enough-entropy-1234567890";
    private readonly AesEncryptionService _enc = new(Key);

    private string Current(string plain) => "ENC:" + _enc.Encrypt(plain);

    [Fact]
    public void String_InPlainText_NeedsMigration()
    {
        var doc = new BsonDocument { { "Name", "Maria" } };

        Assert.True(EncryptionMigrationRules.NeedsMigration("tags", doc));
    }

    [Fact]
    public void String_InTheLegacyCbcFormat_NeedsMigration()
    {
        var doc = new BsonDocument { { "Name", "ENC:" + AesEncryptionServiceTests.LegacyEncrypt(Key, "Maria") } };

        Assert.True(EncryptionMigrationRules.NeedsMigration("tags", doc));
    }

    [Fact]
    public void String_InTheCurrentFormat_DoesNotNeedMigration()
    {
        var doc = new BsonDocument { { "Name", Current("Maria") } };

        Assert.False(EncryptionMigrationRules.NeedsMigration("tags", doc));
    }

    [Fact]
    public void EmptyNullOrMissingStrings_AreIgnored()
    {
        Assert.False(EncryptionMigrationRules.NeedsMigration("tags", new BsonDocument { { "Name", "" } }));
        Assert.False(EncryptionMigrationRules.NeedsMigration("tags", new BsonDocument { { "Name", BsonNull.Value } }));
        Assert.False(EncryptionMigrationRules.NeedsMigration("tags", new BsonDocument()));
    }

    [Fact]
    public void Decimal_StoredAsANumberOrLegacyString_NeedsMigration()
    {
        Assert.True(EncryptionMigrationRules.NeedsMigration("entries", new BsonDocument { { "Value", 12.5 } }));
        Assert.True(EncryptionMigrationRules.NeedsMigration("entries", new BsonDocument { { "Value", new BsonDecimal128(12.5m) } }));
        Assert.True(EncryptionMigrationRules.NeedsMigration("entries",
            new BsonDocument { { "Value", AesEncryptionServiceTests.LegacyEncrypt(Key, "12.50") } }));
    }

    [Fact]
    public void Decimal_InTheCurrentFormat_DoesNotNeedMigration()
    {
        var doc = new BsonDocument { { "Value", _enc.EncryptDecimal(12.5m) } };

        Assert.False(EncryptionMigrationRules.NeedsMigration("entries", doc));
    }

    [Fact]
    public void NullableDecimal_Null_IsIgnored()
    {
        Assert.False(EncryptionMigrationRules.NeedsMigration("users", new BsonDocument { { "DailyBudget", BsonNull.Value } }));
    }

    [Fact]
    public void BirthDate_AsAPlainBsonDate_NeedsMigration_AndEncryptedDoesNot()
    {
        Assert.True(EncryptionMigrationRules.NeedsMigration("users",
            new BsonDocument { { "BirthDate", new BsonDateTime(DateTime.UtcNow) } }));
        Assert.False(EncryptionMigrationRules.NeedsMigration("users",
            new BsonDocument { { "BirthDate", Current("1990-05-17T00:00:00.0000000Z") } }));
    }

    [Fact]
    public void House_NeedsMigrationWhenAMemberIsStillPlain()
    {
        var house = new BsonDocument
        {
            { "Name", Current("Casa") },
            { "Members", new BsonArray { new BsonDocument { { "Name", "Ana" }, { "Email", Current("a@x.com") } } } },
        };

        Assert.True(EncryptionMigrationRules.NeedsMigration("houses", house));
    }

    [Fact]
    public void House_FullyEncrypted_DoesNotNeedMigration()
    {
        var house = new BsonDocument
        {
            { "Name", Current("Casa") },
            { "Description", Current("Apto") },
            { "GoalName", Current("Viagem") },
            { "GoalTargetAmount", _enc.EncryptDecimal(1000m) },
            { "Members", new BsonArray { new BsonDocument { { "Name", Current("Ana") }, { "Email", Current("a@x.com") } } } },
        };

        Assert.False(EncryptionMigrationRules.NeedsMigration("houses", house));
    }

    [Fact]
    public void UnknownCollection_IsNeverMigrated()
    {
        Assert.False(EncryptionMigrationRules.NeedsMigration("refreshtokens", new BsonDocument { { "Token", "x" } }));
    }

    [Fact]
    public void UnreadableValues_AreDetected_SoTheyAreNeverRewritten()
    {
        var otherKey = new AesEncryptionService("a-completely-different-key-1234567890");
        var wrongKey = new BsonDocument { { "Name", "ENC:" + otherKey.Encrypt("segredo") } };
        var garbage = new BsonDocument { { "Name", "ENC:AAAA" } };
        var good = new BsonDocument { { "Name", Current("ok") } };
        var legacy = new BsonDocument { { "Name", "ENC:" + AesEncryptionServiceTests.LegacyEncrypt(Key, "ok") } };
        var plain = new BsonDocument { { "Name", "texto puro" } };

        Assert.True(EncryptionMigrationRules.HasUnreadableValue("tags", wrongKey, _enc.Decrypt));
        Assert.True(EncryptionMigrationRules.HasUnreadableValue("tags", garbage, _enc.Decrypt));
        Assert.False(EncryptionMigrationRules.HasUnreadableValue("tags", good, _enc.Decrypt));
        Assert.False(EncryptionMigrationRules.HasUnreadableValue("tags", legacy, _enc.Decrypt));
        Assert.False(EncryptionMigrationRules.HasUnreadableValue("tags", plain, _enc.Decrypt));
    }

    [Fact]
    public void UnreadableDecimal_IsDetected()
    {
        var otherKey = new AesEncryptionService("a-completely-different-key-1234567890");
        var doc = new BsonDocument { { "Value", otherKey.EncryptDecimal(5m) } };

        Assert.True(EncryptionMigrationRules.HasUnreadableValue("entries", doc, _enc.Decrypt));
    }

    [Fact]
    public void RefreshToken_InPlainText_NeedsHash_AndAHashDoesNot()
    {
        var plain = new BsonDocument { { "Token", Convert.ToBase64String(new byte[64]) } };
        var hashed = new BsonDocument { { "Token", TokenHasher.Hash("abc") } };

        Assert.True(EncryptionMigrationRules.RefreshTokenNeedsHash(plain));
        Assert.False(EncryptionMigrationRules.RefreshTokenNeedsHash(hashed));
    }

    [Fact]
    public void EveryFieldTheContextEncrypts_IsCoveredByTheMigrationRules()
    {
        // Keep in sync with the class maps in ZenoMongoContext.
        var expected = new Dictionary<string, string[]>
        {
            ["users"] = new[] { "Name", "Phone", "Document", "DailyBudget", "BirthDate" },
            ["entries"] = new[] { "Title", "Description", "Value" },
            ["houses"] = new[] { "Name", "Description", "GoalName", "GoalTargetAmount" },
            ["goals"] = new[] { "Name", "TargetAmount", "MonthlyContribution", "InitialAmount" },
            ["tags"] = new[] { "Name" },
            ["monthlyexpensecategories"] = new[] { "Name", "Amount" },
            ["capturerules"] = new[] { "Match" },
            ["pushsubscriptions"] = new[] { "P256dh", "Auth" },
        };

        foreach (var (collection, fields) in expected)
        {
            var actual = EncryptionMigrationRules.Collections[collection].Select(f => f.Name).OrderBy(n => n);
            Assert.Equal(fields.OrderBy(n => n), actual);
        }

        Assert.Equal(expected.Count, EncryptionMigrationRules.Collections.Count);
    }
}
