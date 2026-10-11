using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Zeno.Application.Interfaces;

namespace Zeno.Application.Services;

/// <summary>
/// Transactional email through Resend (https://resend.com). Config: Email:Resend:ApiKey and Email:From.
/// Without a key, Development prints the email to the console so flows can be tested locally;
/// other environments only note that email is not configured (never the content: it carries codes).
/// </summary>
public class ResendEmailSender : IEmailSender
{
    private const string Endpoint = "https://api.resend.com/emails";

    private readonly HttpClient _httpClient;
    private readonly IHostEnvironment _environment;
    private readonly string? _apiKey;
    private readonly string _from;

    public ResendEmailSender(HttpClient httpClient, IConfiguration configuration, IHostEnvironment environment)
    {
        _httpClient = httpClient;
        _environment = environment;
        _apiKey = configuration["Email:Resend:ApiKey"];
        // onboarding@resend.dev works before a domain is verified, but only delivers to the Resend account owner.
        _from = configuration["Email:From"] is { Length: > 0 } from ? from : "Zeno <onboarding@resend.dev>";
    }

    public async Task<bool> SendAsync(string to, string subject, string html, string text)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            if (_environment.IsDevelopment())
                Console.WriteLine($"[Email:dev] To: {to}\nSubject: {subject}\n{text}");
            else
                Console.WriteLine("[Email] Email:Resend:ApiKey is not set; email not sent.");
            return false;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(new { from = _from, to = new[] { to }, subject, html, text }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        try
        {
            using var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode) return true;
            Console.WriteLine($"[Email] Resend returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return false;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[Email] Resend unreachable: {ex.Message}");
            return false;
        }
    }
}
