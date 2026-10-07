using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Zeno.Application.Interfaces;

namespace Zeno.Application.Services;

/// <summary>
/// Criptografia de campos. Formato atual ("v2."): AES-256-GCM com chave derivada por HKDF, nonce
/// aleatório de 12 bytes e tag de autenticação de 16 bytes, o que também detecta adulteração.
/// Valores antigos (sem prefixo; AES-CBC com chave SHA-256) continuam legíveis para a migração.
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    public const string CurrentPrefix = "v2.";

    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const string KeyInfo = "zeno-field-encryption-v2";

    private readonly byte[] _key;
    private readonly byte[] _legacyKey;

    public AesEncryptionService(string key)
    {
        var material = Encoding.UTF8.GetBytes(key);
        _legacyKey = SHA256.HashData(material);
        _key = HKDF.DeriveKey(HashAlgorithmName.SHA256, material, 32, salt: null, info: Encoding.UTF8.GetBytes(KeyInfo));
    }

    /// <summary>true se o valor já está no formato atual (AES-GCM).</summary>
    public static bool IsCurrentFormat(string cipherText)
    {
        return cipherText.StartsWith(CurrentPrefix, StringComparison.Ordinal);
    }

    public string Encrypt(string plainText)
    {
        var plain = Encoding.UTF8.GetBytes(plainText);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];

        using (var gcm = new AesGcm(_key, TagSize))
            gcm.Encrypt(nonce, plain, cipher, tag);

        var payload = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, payload, NonceSize + TagSize, cipher.Length);

        return CurrentPrefix + Convert.ToBase64String(payload);
    }

    public string Decrypt(string cipherText)
    {
        return IsCurrentFormat(cipherText)
            ? DecryptCurrent(cipherText[CurrentPrefix.Length..])
            : DecryptLegacy(cipherText);
    }

    public string EncryptDecimal(decimal value)
    {
        return Encrypt(value.ToString("F2", CultureInfo.InvariantCulture));
    }

    public decimal DecryptDecimal(string cipherText)
    {
        var plain = Decrypt(cipherText);

        // Valores antigos foram gravados com a cultura do servidor, então são lidos do mesmo jeito.
        return IsCurrentFormat(cipherText)
            ? decimal.Parse(plain, NumberStyles.Number, CultureInfo.InvariantCulture)
            : decimal.Parse(plain);
    }

    private string DecryptCurrent(string base64Payload)
    {
        var payload = Convert.FromBase64String(base64Payload);
        if (payload.Length < NonceSize + TagSize)
            throw new CryptographicException("Valor criptografado inválido.");

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var cipher = payload.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];

        using (var gcm = new AesGcm(_key, TagSize))
            gcm.Decrypt(nonce, cipher, tag, plain);

        return Encoding.UTF8.GetString(plain);
    }

    private string DecryptLegacy(string cipherText)
    {
        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _legacyKey;

        var iv = new byte[aes.IV.Length];
        var cipher = new byte[fullCipher.Length - iv.Length];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
