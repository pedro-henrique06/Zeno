using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Zeno.Application.Services;

namespace Zeno.Tests;

public class AesEncryptionServiceTests
{
    private const string Key = "test-key-with-enough-entropy-1234567890";
    private readonly AesEncryptionService _service = new(Key);

    /// <summary>The original format: AES-CBC with SHA-256(key) and a random IV prepended.</summary>
    internal static string LegacyEncrypt(string key, string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        aes.GenerateIV();

        var plain = Encoding.UTF8.GetBytes(plainText);
        var cipher = aes.CreateEncryptor().TransformFinalBlock(plain, 0, plain.Length);

        var result = new byte[aes.IV.Length + cipher.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipher, 0, result, aes.IV.Length, cipher.Length);
        return Convert.ToBase64String(result);
    }

    [Theory]
    [InlineData("Maria da Silva")]
    [InlineData("Água • 日本語 • 🙂")]
    [InlineData("a")]
    [InlineData("")]
    public void Encrypt_UsesTheCurrentFormat_AndRoundTrips(string plain)
    {
        var cipher = _service.Encrypt(plain);

        Assert.StartsWith(AesEncryptionService.CurrentPrefix, cipher);
        Assert.True(AesEncryptionService.IsCurrentFormat(cipher));
        Assert.Equal(plain, _service.Decrypt(cipher));
    }

    [Fact]
    public void Encrypt_SamePlainTextTwice_GivesDifferentCiphertexts()
    {
        Assert.NotEqual(_service.Encrypt("igual"), _service.Encrypt("igual"));
    }

    [Fact]
    public void Encrypt_DoesNotLeakThePlainText()
    {
        var cipher = _service.Encrypt("segredo-muito-especifico");

        Assert.DoesNotContain("segredo", cipher);
    }

    [Fact]
    public void Decrypt_TamperedValue_IsRejected()
    {
        var cipher = _service.Encrypt("saldo 1000");
        var payload = Convert.FromBase64String(cipher[AesEncryptionService.CurrentPrefix.Length..]);
        payload[^1] ^= 0x01;
        var tampered = AesEncryptionService.CurrentPrefix + Convert.ToBase64String(payload);

        Assert.ThrowsAny<CryptographicException>(() => _service.Decrypt(tampered));
    }

    [Fact]
    public void Decrypt_WithAnotherKey_IsRejected()
    {
        var cipher = new AesEncryptionService("another-key-another-key-another-key-1").Encrypt("x");

        Assert.ThrowsAny<CryptographicException>(() => _service.Decrypt(cipher));
    }

    [Fact]
    public void Decrypt_TruncatedValue_IsRejected()
    {
        var tooShort = AesEncryptionService.CurrentPrefix + Convert.ToBase64String(new byte[10]);

        Assert.ThrowsAny<CryptographicException>(() => _service.Decrypt(tooShort));
    }

    [Fact]
    public void Decrypt_LegacyCbcValue_StillWorks()
    {
        var legacy = LegacyEncrypt(Key, "dado antigo");

        Assert.False(AesEncryptionService.IsCurrentFormat(legacy));
        Assert.Equal("dado antigo", _service.Decrypt(legacy));
    }

    [Theory]
    [InlineData("0.00")]
    [InlineData("1234.56")]
    [InlineData("-99.90")]
    [InlineData("1000000000.00")]
    public void Decimal_RoundTrips_AndIsStoredWithInvariantCulture(string text)
    {
        var value = decimal.Parse(text, CultureInfo.InvariantCulture);

        var cipher = _service.EncryptDecimal(value);

        Assert.True(AesEncryptionService.IsCurrentFormat(cipher));
        Assert.Equal(value, _service.DecryptDecimal(cipher));
        Assert.Equal(text, _service.Decrypt(cipher));
    }

    [Fact]
    public void Decimal_DoesNotDependOnTheServerCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            var cipher = _service.EncryptDecimal(1234.56m);

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.Equal(1234.56m, _service.DecryptDecimal(cipher));

            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.Equal(1234.56m, _service.DecryptDecimal(cipher));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Decimal_LegacyValue_IsReadWithTheCultureItWasWritten()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var legacy = LegacyEncrypt(Key, 1234.56m.ToString("F2"));

            Assert.Equal(1234.56m, _service.DecryptDecimal(legacy));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
