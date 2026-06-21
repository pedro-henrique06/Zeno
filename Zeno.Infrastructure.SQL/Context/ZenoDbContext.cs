using System.Data;
using MySqlConnector;

namespace Zeno.Infrastructure.SQL.Context;

public class ZenoDbContext
{
    private readonly string _connectionString;
    private MySqlConnection? _connection;
    private MySqlTransaction? _transaction;

    public ZenoDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public MySqlConnection Connection
    {
        get
        {
            if (_connection is null || _connection.State == System.Data.ConnectionState.Closed)
            {
                _connection = new MySqlConnection(_connectionString);
            }

            return _connection;
        }
    }

    public IDbTransaction? Transaction => _transaction;

    public async Task BeginTransactionAsync()
    {
        if (_connection is null || _connection.State == System.Data.ConnectionState.Closed)
        {
            _connection = new MySqlConnection(_connectionString);
        }
        if (_connection.State == System.Data.ConnectionState.Closed)
        {
            await _connection.OpenAsync();
        }
        _transaction = await _connection.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            _transaction = null;
        }
    }
}