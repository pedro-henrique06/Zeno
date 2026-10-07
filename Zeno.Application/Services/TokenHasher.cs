using System.Security.Cryptography;
using System.Text;

namespace Zeno.Application.Services;

/// <summary>
/// Hash SHA-256 (hex minúsculo) de tokens de alta entropia, como o refresh token. O banco guarda só o
/// hash, então um vazamento da base não entrega tokens utilizáveis.
/// </summary>
public static class TokenHasher
{
    public const int HashLength = 64;

    public static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }

    /// <summary>true se o valor tem a forma de um hash (64 caracteres hexadecimais minúsculos).</summary>
    public static bool IsHash(string value)
    {
        if (value.Length != HashLength)
            return false;

        foreach (var c in value)
        {
            if (!(c is >= '0' and <= '9' || c is >= 'a' and <= 'f'))
                return false;
        }

        return true;
    }
}
