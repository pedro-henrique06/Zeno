namespace Zeno.Application.Services;

/// <summary>
/// Para onde o callback do OAuth devolve o usuario. O app web volta para o dominio do frontend; o app
/// nativo marca a origem com state=app e volta para o esquema do app (zeno://), que o sistema abre de
/// volta no aplicativo.
/// </summary>
public static class OAuthReturnTarget
{
    public const string AppState = "app";
    public const string AppCallback = "zeno://auth/callback";

    public static bool IsApp(string? state) => string.Equals(state, AppState, StringComparison.Ordinal);

    public static string Success(string? state, string frontendBaseUrl, string token, string refreshToken)
    {
        var query = $"token={Uri.EscapeDataString(token)}&refreshToken={Uri.EscapeDataString(refreshToken)}";
        return IsApp(state) ? $"{AppCallback}?{query}" : $"{frontendBaseUrl}/auth/callback?{query}";
    }

    public static string Failure(string? state, string frontendBaseUrl) =>
        IsApp(state) ? $"{AppCallback}?error=1" : $"{frontendBaseUrl}/login?oauthError=1";
}
