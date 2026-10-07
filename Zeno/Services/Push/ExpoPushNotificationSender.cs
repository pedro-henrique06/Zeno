using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Zeno.Application.Interfaces;
using Zeno.Application.Notifications;

namespace Zeno.Services.Push;

/// <summary>
/// Envia pelo servico de push do Expo, que entrega via APNs/FCM sem o backend precisar de credencial do
/// Firebase. Serve os tokens "ExponentPushToken[...]" gerados pelo app nativo.
/// </summary>
public class ExpoPushNotificationSender : IPushNotificationSender
{
    public const string Endpoint = "https://exp.host/--/api/v2/push/send";
    private const int BatchSize = 100;

    private readonly HttpClient _httpClient;
    private readonly ExpoOptions _options;
    private readonly ILogger<ExpoPushNotificationSender> _logger;

    public ExpoPushNotificationSender(HttpClient httpClient, IOptions<PushOptions> options, ILogger<ExpoPushNotificationSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value.Expo;
        _logger = logger;
    }

    /// <summary>O servico do Expo nao exige credencial; o AccessToken so e usado se a seguranca reforcada estiver ligada no projeto.</summary>
    public bool IsConfigured => true;

    public static bool IsExpoToken(string token) =>
        token.StartsWith("ExponentPushToken[", StringComparison.Ordinal) ||
        token.StartsWith("ExpoPushToken[", StringComparison.Ordinal);

    public async Task<PushSendResult> SendAsync(IReadOnlyCollection<string> tokens, PushMessage message, CancellationToken cancellationToken = default)
    {
        if (tokens.Count == 0)
            return PushSendResult.Empty;

        var success = 0;
        var invalid = new List<string>();

        foreach (var batch in tokens.Chunk(BatchSize))
        {
            var payload = batch.Select(token => new
            {
                to = token,
                title = message.Title,
                body = message.Body,
                data = message.Data,
                sound = "default"
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrWhiteSpace(_options.AccessToken))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.AccessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Falha ao enviar push pelo Expo ({Status}): {Body}", (int)response.StatusCode, body);
                continue;
            }

            var parsed = ExpoTicketParser.Parse(body, batch);
            success += parsed.Success;
            invalid.AddRange(parsed.Invalid);
        }

        return new PushSendResult { SuccessCount = success, InvalidTokens = invalid };
    }
}
