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
        services.AddSingleton<IEmailBlindIndex>(_ => new EmailBlindIndex(encryptionKey));
        services.AddSingleton<ZenoMongoContext>(sp => new ZenoMongoContext(connectionString, sp.GetRequiredService<IEncryptionService>()));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEntryRepository, EntryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IMonthlyExpenseCategoryRepository, MonthlyExpenseCategoryRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();
        services.AddScoped<IHouseRepository, HouseRepository>();
        services.AddScoped<IGoalRepository, GoalRepository>();
        services.AddScoped<IWidgetKeyRepository, WidgetKeyRepository>();
        services.AddScoped<ICaptureKeyRepository, CaptureKeyRepository>();
        services.AddScoped<ICaptureRuleRepository, CaptureRuleRepository>();
        services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();

        return services;
    }
}
