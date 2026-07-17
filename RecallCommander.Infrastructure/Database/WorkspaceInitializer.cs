using Dapper;
using Microsoft.Data.Sqlite;
using RecallCommander.Contracts.Workspace;

namespace RecallCommander.Infrastructure.Database;

/// <summary>
/// Creates the metadata database and its schema. Idempotent: running it against
/// an existing workspace changes nothing.
/// </summary>
public sealed class WorkspaceInitializer(IDataPaths dataPaths) : IWorkspaceInitializer
{
    private const string Schema =
        """
        CREATE TABLE IF NOT EXISTS question_sources (
            id                INTEGER PRIMARY KEY AUTOINCREMENT,
            directory_path    TEXT    NOT NULL UNIQUE,
            registered_at_utc TEXT    NOT NULL
        );

        CREATE TABLE IF NOT EXISTS app_settings (
            key   TEXT PRIMARY KEY,
            value TEXT NOT NULL
        );

        INSERT INTO app_settings (key, value)
        VALUES ('schema_version', '1')
        ON CONFLICT (key) DO NOTHING;
        """;

    public async Task<InitializationResult> InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(dataPaths.DataDirectory);
        var existed = File.Exists(dataPaths.DatabasePath);

        await using var connection = new SqliteConnection($"Data Source={dataPaths.DatabasePath}");
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(Schema, cancellationToken: cancellationToken));

        return new InitializationResult(Created: !existed, dataPaths.DatabasePath);
    }
}
