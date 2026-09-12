using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Zeno.Application.Interfaces;
using Zeno.Domain.Enum;

namespace Zeno.Application.Services;

public class ExchangeRateService : IExchangeRateService
{
    private readonly HttpClient _httpClient;

    public ExchangeRateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<decimal> GetRateAsync(Currency from, Currency to)
    {
        if (from == to)
            return 1m;

        var response = await _httpClient.GetFromJsonAsync<FrankfurterResponse>(
            $"https://api.frankfurter.app/latest?from={from}&to={to}");

        if (response?.Rates is null || !response.Rates.TryGetValue(to.ToString(), out var rate))
            throw new InvalidOperationException("Não foi possível obter a taxa de câmbio.");

        return rate;
    }

    private class FrankfurterResponse
    {
        [JsonPropertyName("rates")]
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}
