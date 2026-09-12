namespace Zeno.Application.Notifications;

public sealed class PushMessage
{
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;

    /// <summary>Pares chave/valor entregues ao app junto da notificacao (deep link, tipo do aviso, etc).</summary>
    public IReadOnlyDictionary<string, string> Data { get; init; } = new Dictionary<string, string>();
}

public sealed class PushSendResult
{
    public int SuccessCount { get; init; }

    /// <summary>Tokens recusados de forma definitiva pelo provedor (app desinstalado, token expirado). Devem ser desativados.</summary>
    public IReadOnlyCollection<string> InvalidTokens { get; init; } = Array.Empty<string>();

    public static PushSendResult Empty => new();
}
