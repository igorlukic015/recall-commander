using RecallCommander.Application.Abstractions;
using RecallCommander.Application.Artifacts;
using RecallCommander.Domain;
using RecallCommander.Infrastructure.Database;

namespace RecallCommander.IntegrationTests.Support;

/// <summary>Points the SQLite database into a test workspace.</summary>
public sealed class TestDataPaths(string dataDirectory) : IDataPaths
{
    public string DataDirectory => dataDirectory;

    public string DatabasePath => Path.Combine(dataDirectory, "recall.db");
}

/// <summary>Writes artifacts into a test workspace instead of the process working directory.</summary>
public sealed class FixedArtifactOutputPathProvider(string directory) : IArtifactOutputPathProvider
{
    public string GetOutputDirectory() => directory;
}

/// <summary>A frozen clock for deterministic file names and frontmatter.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

/// <summary>
/// Fixed source list for integrations where the repository is not the
/// component under test (e.g. scanner + parser + filesystem).
/// </summary>
public sealed class StubQuestionSourceRepository(params string[] directories) : IQuestionSourceRepository
{
    private readonly List<QuestionSource> _sources = directories
        .Select((directory, index) => new QuestionSource(index + 1, directory, DateTimeOffset.UtcNow))
        .ToList();

    public Task<QuestionSource> AddAsync(
        string directoryPath,
        DateTimeOffset registeredAtUtc,
        CancellationToken cancellationToken = default)
    {
        var source = new QuestionSource(_sources.Count + 1, directoryPath, registeredAtUtc);
        _sources.Add(source);
        return Task.FromResult(source);
    }

    public Task<IReadOnlyList<QuestionSource>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<QuestionSource>>(_sources.ToList());

    public Task<bool> ExistsAsync(string directoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sources.Any(source => source.DirectoryPath == directoryPath));
}
