using System.Globalization;
using Dapper;
using RecallCommander.Application.Abstractions;
using RecallCommander.Domain;

namespace RecallCommander.Infrastructure.Database;

public sealed class SqliteQuestionSourceRepository(ISqliteConnectionFactory connectionFactory)
    : IQuestionSourceRepository
{
    public async Task<QuestionSource> AddAsync(
        string directoryPath,
        DateTimeOffset registeredAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();

        var id = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            """
            INSERT INTO question_sources (directory_path, registered_at_utc)
            VALUES (@DirectoryPath, @RegisteredAtUtc);
            SELECT last_insert_rowid();
            """,
            new { DirectoryPath = directoryPath, RegisteredAtUtc = Format(registeredAtUtc) },
            cancellationToken: cancellationToken));

        return new QuestionSource(id, directoryPath, registeredAtUtc);
    }

    public async Task<IReadOnlyList<QuestionSource>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();

        var rows = await connection.QueryAsync<QuestionSourceRow>(new CommandDefinition(
            """
            SELECT id                AS Id,
                   directory_path    AS DirectoryPath,
                   registered_at_utc AS RegisteredAtUtc
            FROM question_sources
            ORDER BY id
            """,
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToDomain()).ToList();
    }

    public async Task<bool> ExistsAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM question_sources WHERE directory_path = @DirectoryPath)",
            new { DirectoryPath = directoryPath },
            cancellationToken: cancellationToken));
    }

    private static string Format(DateTimeOffset timestamp) =>
        timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

    private sealed record QuestionSourceRow(long Id, string DirectoryPath, string RegisteredAtUtc)
    {
        public QuestionSource ToDomain() => new(
            Id,
            DirectoryPath,
            DateTimeOffset.Parse(RegisteredAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
    }
}
