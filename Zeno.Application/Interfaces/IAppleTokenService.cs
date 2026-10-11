namespace Zeno.Application.Interfaces;

/// <summary>
/// Server-to-server calls to Sign in with Apple: trade the app's one-time authorization code for a
/// refresh token at sign-in, and revoke it when the account is deleted (App Store guideline 5.1.1(v)).
/// Needs OAuth:Apple:TeamId, KeyId and PrivateKey (the .p8); without them both calls are no-ops.
/// </summary>
public interface IAppleTokenService
{
    bool IsConfigured { get; }

    /// <summary>Returns Apple's refresh token for the code, or null if not configured or Apple refused.</summary>
    Task<string?> ExchangeCodeAsync(string authorizationCode);

    /// <summary>Revokes the token at Apple; false if not configured or the call failed.</summary>
    Task<bool> RevokeAsync(string refreshToken);
}
