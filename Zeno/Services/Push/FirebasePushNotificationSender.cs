using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Zeno.Application.Interfaces;
using Zeno.Application.Notifications;

namespace Zeno.Services.Push;

/// <summary>
/// Envia via FCM HTTP v1. A autenticacao usa a service account do Firebase: assinamos um JWT
/// com a chave privada e trocamos por um access token no endpoint OAuth do Google.
/// A API legada de server key foi desligada pelo Google, por isso o fluxo v1.
/// </summary>
public class FirebasePushNotificationSender : IPushNotificationSender
{
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string Scope = "https://www.googleapis.com/auth/firebase.messaging";

    private readonly HttpClient _httpClient;
    private readonly FirebaseOptions _options;
    private readonly ILogger<FirebasePushNotificationSender> _logger;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _accessToken;
    private DateTime _accessTokenExpiresAtUtc = DateTime.MinValue;

    public FirebasePushNotificationSender(
        HttpClient httpClient,
        IOptions<PushOptions> options,
        ILogger<FirebasePushNotificationSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value.Firebase;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsComplete;

    public async Task<PushSendResult> SendAsync(IReadOnlyCollection<string> tokens, PushMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || tokens.Count == 0)
            return PushSendResult.Empty;

        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var invalid = new List<string>();
        var success = 0;

        foreach (var token in tokens)
        {
            var outcome = await SendSingleAsync(token, message, accessToken, cancellationToken);

            if (outcome == SendOutcome.Success)
                success++;
            else if (outcome == SendOutcome.InvalidToken)
                invalid.Add(token);
        }

        return new PushSendResult { SuccessCount = success, InvalidTokens = invalid };
    }

    private async Task<SendOutcome> SendSingleAsync(string deviceToken, PushMessage message, string accessToken, CancellationToken cancellationToken)
    {
        var payload = new
        {
            message = new
            {
                token = deviceToken,
                notification = new { title = message.Title, body = message.Body },
                data = message.Data,
                // sound explicito garante que o iOS mostre o alerta com o app em background.
                apns = new { payload = new { aps = new { sound = "default" } } },
                android = new { priority = "high", notification = new { sound = "default" } }
            }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://fcm.googleapis.com/v1/projects/{_options.ProjectId}/messages:send")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
            return SendOutcome.Success;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (IsPermanentTokenFailure(response.StatusCode, body))
        {
            _logger.LogInformation("Token de aparelho invalido, sera desativado. Resposta do FCM: {Body}", body);
            return SendOutcome.InvalidToken;
        }

        _logger.LogError("Falha ao enviar push ({Status}): {Body}", (int)response.StatusCode, body);
        return SendOutcome.TransientFailure;
    }

    /// <summary>NOT_FOUND/UNREGISTERED significa app desinstalado ou token rotacionado; INVALID_ARGUMENT em token malformado.</summary>
    private static bool IsPermanentTokenFailure(HttpStatusCode status, string body)
    {
        if (status == HttpStatusCode.NotFound)
            return true;

        return status == HttpStatusCode.BadRequest &&
               (body.Contains("INVALID_ARGUMENT", StringComparison.Ordinal) ||
                body.Contains("UNREGISTERED", StringComparison.Ordinal));
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        // Uma margem de 60s evita usar um token que expira durante o lote de envios.
        if (_accessToken is not null && DateTime.UtcNow < _accessTokenExpiresAtUtc.AddSeconds(-60))
            return _accessToken;

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken is not null && DateTime.UtcNow < _accessTokenExpiresAtUtc.AddSeconds(-60))
                return _accessToken;

            var assertion = BuildSignedJwt();

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = assertion
            });

            using var response = await _httpClient.PostAsync(TokenEndpoint, content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Nao foi possivel obter o access token do Google ({(int)response.StatusCode}): {body}");

            using var document = JsonDocument.Parse(body);
            var token = document.RootElement.GetProperty("access_token").GetString()!;
            var expiresIn = document.RootElement.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600;

            _accessToken = token;
            _accessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn);

            return token;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private string BuildSignedJwt()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT" }));
        var claims = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = _options.ClientEmail,
            scope = Scope,
            aud = TokenEndpoint,
            iat = now,
            exp = now + 3600
        }));

        var unsigned = $"{header}.{claims}";

        using var rsa = RSA.Create();
        rsa.ImportFromPem(NormalizePem(_options.PrivateKey));

        var signature = rsa.SignData(Encoding.UTF8.GetBytes(unsigned), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return $"{unsigned}.{Base64Url(signature)}";
    }

    /// <summary>O JSON da service account traz a chave com \n escapado; variaveis de ambiente costumam repetir isso.</summary>
    private static string NormalizePem(string privateKey) => privateKey.Replace("\\n", "\n").Trim();

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private enum SendOutcome
    {
        Success,
        InvalidToken,
        TransientFailure
    }
}
