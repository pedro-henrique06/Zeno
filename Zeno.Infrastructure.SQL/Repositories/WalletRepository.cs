using Dapper;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Wallet;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly ZenoDbContext _context;

    public WalletRepository(ZenoDbContext context)
    {
        _context = context;
    }

    public async Task<Wallet?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT Id, Name, Description, InitialBalance, CreatedAt 
                             FROM Wallets WHERE Id = @Id";

        return await _context.Connection.QueryFirstOrDefaultAsync<Wallet>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Wallet>> GetAllAsync()
    {
        const string sql = @"SELECT Id, Name, Description, InitialBalance, CreatedAt 
                             FROM Wallets ORDER BY CreatedAt DESC";

        return await _context.Connection.QueryAsync<Wallet>(sql);
    }

    public async Task<Wallet> CreateAsync(Wallet wallet)
    {
        const string sql = @"INSERT INTO Wallets (Id, Name, Description, InitialBalance, CreatedAt) 
                             VALUES (@Id, @Name, @Description, @InitialBalance, @CreatedAt)";

        await _context.Connection.ExecuteAsync(sql, new
        {
            wallet.Id,
            wallet.Name,
            wallet.Description,
            wallet.InitialBalance,
            wallet.CreatedAt
        });

        return wallet;
    }

    public async Task<Wallet> UpdateAsync(Wallet wallet)
    {
        const string sql = @"UPDATE Wallets 
                             SET Name = @Name, Description = @Description, InitialBalance = @InitialBalance 
                             WHERE Id = @Id";

        await _context.Connection.ExecuteAsync(sql, new
        {
            wallet.Id,
            wallet.Name,
            wallet.Description,
            wallet.InitialBalance
        });

        return wallet;
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"DELETE FROM Wallets WHERE Id = @Id";

        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }
}
