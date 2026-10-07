using Zeno.Application.Services;

namespace Zeno.Tests;

public class TokenHasherTests
{
    [Fact]
    public void Hash_IsDeterministic_64LowercaseHex_AndDoesNotContainTheToken()
    {
        var hash = TokenHasher.Hash("meu-refresh-token");

        Assert.Equal(hash, TokenHasher.Hash("meu-refresh-token"));
        Assert.Equal(64, hash.Length);
        Assert.True(TokenHasher.IsHash(hash));
        Assert.DoesNotContain("refresh", hash);
    }

    [Fact]
    public void Hash_DiffersForDifferentTokens()
    {
        Assert.NotEqual(TokenHasher.Hash("a"), TokenHasher.Hash("b"));
    }

    [Fact]
    public void IsHash_IsFalseForARealRefreshToken()
    {
        // Real refresh tokens are 64 random bytes in base64 (88 characters).
        var token = Convert.ToBase64String(new byte[64]);

        Assert.False(TokenHasher.IsHash(token));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789")] // uppercase
    [InlineData("zzcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789")] // not hex
    public void IsHash_RejectsOtherShapes(string value)
    {
        Assert.False(TokenHasher.IsHash(value));
    }
}
