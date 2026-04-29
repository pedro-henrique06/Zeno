using Microsoft.Extensions.DependencyInjection;
using Zeno.Application.Interfaces;
using Zeno.Application.Services;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;
using Zeno.Infrastructure.SQL.Repositories;

namespace Zeno.Infrastructure.SQL.Extentions;

public static class ServiceConfigurator
{
    public static IServiceCollection AddInfrastructureSQL(this IServiceCollection services, string connectionString, string encryptionKey)
    {
        services.AddSingleton<IEncryptionService>(_ => new AesEncryptionService(encryptionKey));
        services.AddScoped<ZenoDbContext>(_ => new ZenoDbContext(connectionString));
        services.AddScoped<IEntryRepository, EntryRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IHomeRepository, HomeRepository>();
        services.AddScoped<ISalaryRepository, SalaryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
