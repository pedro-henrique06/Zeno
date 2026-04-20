using Microsoft.Data.SqlClient;

namespace Zeno.Infrastructure.SQL.Context;

public class ZenoDbContext
{
    private readonly string _connectionString;
    private SqlConnection? _connection;

    public ZenoDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SqlConnection Connection
    {
        get
        {
            if (_connection is null || _connection.State == System.Data.ConnectionState.Closed)
            {
                _connection = new SqlConnection(_connectionString);
            }

            return _connection;
        }
    }
}
