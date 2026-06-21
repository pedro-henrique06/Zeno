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
        services.AddSingleton<ZenoMongoContext>(_ => new ZenoMongoContext(connectionString));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEntryRepository, EntryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFinancialGoalRepository, FinancialGoalRepository>();
        services.AddScoped<IDebtRepository, DebtRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICategoryRuleRepository, CategoryRuleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        return services;
    }
}
