using Microsoft.Data.Sqlite;
using RecallCommander.Contracts.Exceptions;

namespace RecallCommander.Infrastructure.Database;

public interface ISqliteConnectionFactory
{
    SqliteConnection CreateOpenConnection();
}

/// <summary>
/// Opens connections to the metadata database. Requires the workspace to be
/// initialized; repositories rely on this guard.
/// </summary>
public sealed class SqliteConnectionFactory(IDataPaths dataPaths) : ISqliteConnectionFactory
{
    public SqliteConnection CreateOpenConnection()
    {
        if (!File.Exists(dataPaths.DatabasePath))
        {
            throw new WorkspaceNotInitializedException();
        }

        var connection = new SqliteConnection($"Data Source={dataPaths.DatabasePath}");
        connection.Open();
        return connection;
    }
}
