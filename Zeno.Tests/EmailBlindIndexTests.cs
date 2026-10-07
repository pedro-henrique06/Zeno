using Zeno.Application.Services;
using Zeno.Infrastructure.SQL.Serialization;
using MongoDB.Bson;

namespace Zeno.Tests;

public class EmailBlindIndexTests
{
    private const string Key = "unit-test-key-unit-test-key-unit-test-key";

    [Fact]
    public void Compute_IgnoresCaseAndSurroundingSpaces()
    {
        var index = new EmailBlindIndex(Key);
        Assert.Equal(index.Compute("ana@exemplo.com"), index.Compute("  Ana@Exemplo.COM "));
    }

    [Fact]
    public void Compute_DiffersPerEmailAndPerKey()
    {
        var index = new EmailBlindIndex(Key);
        Assert.NotEqual(index.Compute("a@x.com"), index.Compute("b@x.com"));
        Assert.NotEqual(index.Compute("a@x.com"), new EmailBlindIndex("another-key-another-key-another-key-1").Compute("a@x.com"));
    }

    [Fact]
    public void Compute_DoesNotContainTheEmail()
    {
        var hash = new EmailBlindIndex(Key).Compute("ana@exemplo.com");
        Assert.DoesNotContain("ana", hash);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void UserWithoutEmailHash_NeedsMigration_EvenWhenEverythingElseIsCurrent()
    {
        var enc = new AesEncryptionService(Key);
        var doc = new BsonDocument { ["Email"] = "ENC:" + enc.Encrypt("a@x.com") };
        Assert.True(EncryptionMigrationRules.NeedsMigration("users", doc));

        doc["EmailHash"] = new EmailBlindIndex(Key).Compute("a@x.com");
        Assert.False(EncryptionMigrationRules.NeedsMigration("users", doc));
    }

    [Fact]
    public void PlainTextEmail_NeedsMigration()
    {
        var doc = new BsonDocument { ["Email"] = "a@x.com", ["EmailHash"] = "abc" };
        Assert.True(EncryptionMigrationRules.NeedsMigration("users", doc));
    }
}
