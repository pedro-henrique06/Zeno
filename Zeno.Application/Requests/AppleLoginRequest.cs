namespace Zeno.Application.Requests;

public class AppleLoginRequest
{
    /// <summary>The JWT Sign in with Apple returns to the app.</summary>
    public string IdentityToken { get; set; } = string.Empty;

    /// <summary>Apple only sends the name on the very first sign-in, and only to the app, not in the token.</summary>
    public string? FullName { get; set; }
}
