namespace Zeno.Application.Interfaces;

/// <summary>Who Apple says signed in: the stable per-app user id and, when shared, their email.</summary>
public sealed record AppleIdentity(string Subject, string? Email, bool EmailVerified);

public interface IAppleIdentityTokenValidator
{
    /// <summary>
    /// Checks an identity token from Sign in with Apple (signature against Apple's published keys,
    /// issuer, audience, expiry) and returns who it identifies. Throws AppValidationException if invalid.
    /// </summary>
    Task<AppleIdentity> ValidateAsync(string identityToken);
}
