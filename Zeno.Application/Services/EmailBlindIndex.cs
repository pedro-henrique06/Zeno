using System.Security.Cryptography;
using System.Text;
using Zeno.Application.Interfaces;

namespace Zeno.Application.Services;

/// <summary>HMAC-SHA256 do e-mail normalizado (sem espaços, minúsculo), com chave derivada da chave de criptografia.</summary>
public class EmailBlindIndex : IEmailBlindIndex
{
    private const string KeyInfo = "zeno-email-blind-index-v1";

    private readonly byte[] _key;

    public EmailBlindIndex(string key)
    {
        _key = HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(key), 32, salt: null, info: Encoding.UTF8.GetBytes(KeyInfo));
    }

    public string Compute(string email)
    {
        var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
        return Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
    }
}
