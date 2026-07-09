using Microsoft.Extensions.Configuration;
using WebPush;
using Zeno.Application.Interfaces;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;
using DomainPushSubscription = Zeno.Domain.Push.PushSubscription;

namespace Zeno.Application.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly IPushSubscriptionRepository _subscriptionRepository;
    private readonly IBalanceService _balanceService;
    private readonly IUserRepository _userRepository;
    private readonly string _vapidPublicKey;
    private readonly string _vapidPrivateKey;
    private readonly string _vapidSubject;

    public PushNotificationService(
        IPushSubscriptionRepository subscriptionRepository,
        IBalanceService balanceService,
        IUserRepository userRepository,
        IConfiguration configuration)
    {
        _subscriptionRepository = subscriptionRepository;
        _balanceService = balanceService;
        _userRepository = userRepository;
        _vapidPublicKey = configuration["Vapid:PublicKey"] ?? throw new InvalidOperationException("Vapid:PublicKey not configured");
        _vapidPrivateKey = configuration["Vapid:PrivateKey"] ?? throw new InvalidOperationException("Vapid:PrivateKey not configured");
        _vapidSubject = configuration["Vapid:Subject"] ?? "mailto:zeno@app.com";
    }

    public async Task SubscribeAsync(Guid userId, DomainPushSubscription subscription)
    {
        subscription.UserId = userId;
        await _subscriptionRepository.UpsertAsync(subscription);
    }

    public async Task UnsubscribeAsync(Guid userId, string endpoint)
    {
        await _subscriptionRepository.DeleteByEndpointAsync(endpoint);
    }

    public async Task SendDailyBalancesAsync()
    {
        var subscriptions = await _subscriptionRepository.GetAllAsync();
        if (subscriptions.Count == 0) return;

        var client = new WebPushClient();
        client.SetVapidDetails(_vapidSubject, _vapidPublicKey, _vapidPrivateKey);

        var today = DateTime.UtcNow;
        var userGroups = subscriptions.GroupBy(s => s.UserId);

        foreach (var group in userGroups)
        {
            try
            {
                var userId = group.Key;
                var user = await _userRepository.GetByIdAsync(userId);
                if (user is null) continue;

                var balances = await _balanceService.GetMonthlyBalances(userId, today.Month, today.Year);
                var todayDay = balances.Days.FirstOrDefault(d => d.IsToday);
                if (todayDay is null) continue;

                var symbol = user.Currency switch
                {
                    Currency.USD => "$",
                    Currency.EUR => "€",
                    _ => "R$"
                };

                var balance = $"{symbol} {todayDay.Balance:N2}";
                var entrada = todayDay.Entrada > 0 ? $"+{symbol} {todayDay.Entrada:N2}" : null;
                var saida = todayDay.Saida > 0 ? $"-{symbol} {todayDay.Saida:N2}" : null;

                var lines = new List<string>();
                if (entrada is not null) lines.Add($"Entrada: {entrada}");
                if (saida is not null) lines.Add($"Saída: {saida}");
                lines.Add($"Saldo: {balance}");

                var payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    title = "Zeno — Resumo do dia",
                    body = string.Join(" | ", lines),
                    url = "/"
                });

                foreach (var sub in group)
                {
                    try
                    {
                        var pushSub = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                        await client.SendNotificationAsync(pushSub, payload);
                    }
                    catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        await _subscriptionRepository.DeleteByEndpointAsync(sub.Endpoint);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Push] Failed to send to {sub.Endpoint}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Push] Failed for user {group.Key}: {ex.Message}");
            }
        }
    }
}
