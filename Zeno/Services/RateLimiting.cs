using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Zeno.Services;

/// <summary>
/// Limites por IP para os endpoints que funcionam sem login (login, cadastro, refresh e as chaves de
/// widget/captura), onde alguem poderia tentar senhas ou chaves em massa. Os valores sao folgados para uso
/// normal e apertados para automacao abusiva.
/// </summary>
public static class RateLimiting
{
    public const string Auth = "auth";
    public const string Refresh = "refresh";
    public const string Key = "key";

    public static IServiceCollection AddZenoRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync("{\"error\":\"Muitas tentativas. Aguarde um instante e tente de novo.\"}", token);
            };

            options.AddPolicy(Auth, http => PerIp(http, permitLimit: 10, window: TimeSpan.FromMinutes(1)));
            options.AddPolicy(Refresh, http => PerIp(http, permitLimit: 30, window: TimeSpan.FromMinutes(1)));
            options.AddPolicy(Key, http => PerIp(http, permitLimit: 60, window: TimeSpan.FromMinutes(1)));
        });

        return services;
    }

    private static RateLimitPartition<string> PerIp(HttpContext http, int permitLimit, TimeSpan window)
    {
        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueLimit = 0
        });
    }
}
