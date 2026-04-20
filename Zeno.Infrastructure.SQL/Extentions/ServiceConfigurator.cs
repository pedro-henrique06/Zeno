using Microsoft.Extensions.DependencyInjection;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;
using Zeno.Infrastructure.SQL.Repositories;

namespace Zeno.Infrastructure.SQL.Extentions;

public static class ServiceConfigurator
{
    public static IServiceCollection AddInfrastructureSQL(this IServiceCollection services, string connectionString)
    {
        services.AddScoped<ZenoDbContext>(_ => new ZenoDbContext(connectionString));
        services.AddScoped<IEntryRepository, EntryRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();

        return services;
    }
}
