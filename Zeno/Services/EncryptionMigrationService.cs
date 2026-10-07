using MongoDB.Bson;
using MongoDB.Driver;
using Zeno.Application.Interfaces;
using Zeno.Application.Services;
using Zeno.Infrastructure.SQL.Context;
using Zeno.Infrastructure.SQL.Serialization;

namespace Zeno.Services;

/// <summary>
/// Converte, uma vez, os dados que ainda estão em texto puro ou no formato antigo (AES-CBC) para o formato
/// atual (AES-GCM) e troca os refresh tokens em texto puro pelo hash. Só roda quando
/// Encryption__Migration é "dry-run" (apenas conta e registra) ou "run" (regrava). É idempotente: dá para
/// repetir, e documentos já convertidos são ignorados. Faça um backup do banco antes de usar "run".
/// </summary>
public class EncryptionMigrationService : BackgroundService
{
    private const string DryRun = "dry-run";
    private const string Run = "run";

    private readonly ZenoMongoContext _context;
    private readonly IEncryptionService _encryption;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EncryptionMigrationService> _logger;

    public EncryptionMigrationService(
        ZenoMongoContext context,
        IEncryptionService encryption,
        IConfiguration configuration,
        ILogger<EncryptionMigrationService> logger)
    {
        _context = context;
        _encryption = encryption;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mode = _configuration["Encryption:Migration"]?.Trim().ToLowerInvariant();
        if (mode is not (DryRun or Run))
            return;

        try
        {
            // Let the app finish starting (and create indexes) before touching data.
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            _logger.LogWarning("[EncryptionMigration] Starting in '{Mode}' mode.", mode);

            await MigrateAsync("users", _context.Users, mode, stoppingToken);
            await MigrateAsync("entries", _context.Entries, mode, stoppingToken);
            await MigrateAsync("houses", _context.Houses, mode, stoppingToken);
            await MigrateAsync("goals", _context.Goals, mode, stoppingToken);
            await MigrateAsync("tags", _context.Tags, mode, stoppingToken);
            await MigrateAsync("monthlyexpensecategories", _context.MonthlyExpenseCategories, mode, stoppingToken);
            await MigrateAsync("capturerules", _context.CaptureRules, mode, stoppingToken);
            await MigrateAsync("pushsubscriptions", _context.PushSubscriptions, mode, stoppingToken);
            await HashRefreshTokensAsync(mode, stoppingToken);

            _logger.LogWarning("[EncryptionMigration] Finished. Set Encryption__Migration back to empty.");
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EncryptionMigration] Aborted by an unexpected error.");
        }
    }

    private async Task MigrateAsync<T>(string name, IMongoCollection<T> typed, string mode, CancellationToken ct)
    {
        var raw = _context.GetRawCollection(name);
        int scanned = 0, pending = 0, migrated = 0, unreadable = 0, failed = 0;

        using var cursor = await raw.Find(FilterDefinition<BsonDocument>.Empty).ToCursorAsync(ct);
        while (await cursor.MoveNextAsync(ct))
        {
            foreach (var document in cursor.Current)
            {
                scanned++;

                if (EncryptionMigrationRules.HasUnreadableValue(name, document, _encryption.Decrypt))
                {
                    unreadable++;
                    _logger.LogError("[EncryptionMigration] {Collection} {Id}: has values that cannot be decrypted (wrong key?). Skipped.",
                        name, document["_id"]);
                    continue;
                }

                if (!EncryptionMigrationRules.NeedsMigration(name, document))
                    continue;

                pending++;
                if (mode != Run)
                    continue;

                try
                {
                    var filter = new BsonDocumentFilterDefinition<T>(new BsonDocument("_id", document["_id"]));
                    var entity = await typed.Find(filter).FirstOrDefaultAsync(ct);
                    if (entity is null)
                        continue;

                    // Loading through the class map decrypts (or passes plain values through); writing encrypts.
                    await typed.ReplaceOneAsync(filter, entity, cancellationToken: ct);

                    var after = await raw.Find(new BsonDocument("_id", document["_id"])).FirstOrDefaultAsync(ct);
                    if (after is not null && EncryptionMigrationRules.NeedsMigration(name, after))
                        failed++;
                    else
                        migrated++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed++;
                    _logger.LogError(ex, "[EncryptionMigration] {Collection} {Id}: failed.", name, document["_id"]);
                }
            }
        }

        _logger.LogWarning(
            "[EncryptionMigration] {Collection}: scanned={Scanned} pending={Pending} migrated={Migrated} unreadable={Unreadable} failed={Failed}",
            name, scanned, pending, migrated, unreadable, failed);
    }

    private async Task HashRefreshTokensAsync(string mode, CancellationToken ct)
    {
        var raw = _context.GetRawCollection("refreshtokens");
        int scanned = 0, pending = 0, migrated = 0;

        using var cursor = await raw.Find(FilterDefinition<BsonDocument>.Empty).ToCursorAsync(ct);
        while (await cursor.MoveNextAsync(ct))
        {
            foreach (var document in cursor.Current)
            {
                scanned++;
                if (!EncryptionMigrationRules.RefreshTokenNeedsHash(document))
                    continue;

                pending++;
                if (mode != Run)
                    continue;

                var hash = TokenHasher.Hash(document["Token"].AsString);
                var update = Builders<BsonDocument>.Update.Set("Token", hash);
                await raw.UpdateOneAsync(new BsonDocument("_id", document["_id"]), update, cancellationToken: ct);
                migrated++;
            }
        }

        _logger.LogWarning("[EncryptionMigration] refreshtokens: scanned={Scanned} pending={Pending} hashed={Migrated}",
            scanned, pending, migrated);
    }
}
