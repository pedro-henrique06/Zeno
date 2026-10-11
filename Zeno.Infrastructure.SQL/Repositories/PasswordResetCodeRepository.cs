using MongoDB.Driver;
using Zeno.Domain.Auth;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class PasswordResetCodeRepository : IPasswordResetCodeRepository
{
    private readonly ZenoMongoContext _context;

    public PasswordResetCodeRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<PasswordResetCode?> GetByUserAsync(Guid userId)
    {
        return await _context.PasswordResetCodes.Find(x => x.UserId == userId).FirstOrDefaultAsync();
    }

    public async Task ReplaceAsync(PasswordResetCode code)
    {
        await _context.PasswordResetCodes.DeleteManyAsync(x => x.UserId == code.UserId);
        await _context.PasswordResetCodes.InsertOneAsync(code);
    }

    public async Task IncrementAttemptsAsync(Guid id)
    {
        var update = Builders<PasswordResetCode>.Update.Inc(x => x.Attempts, 1);
        await _context.PasswordResetCodes.UpdateOneAsync(x => x.Id == id, update);
    }

    public async Task DeleteByUserAsync(Guid userId)
    {
        await _context.PasswordResetCodes.DeleteManyAsync(x => x.UserId == userId);
    }
}
