using RecallCommander.Contracts.Exceptions;
using RecallCommander.Contracts.Workspace;
using RecallCommander.Domain;
using RecallCommander.Infrastructure.Database;
using RecallCommander.IntegrationTests.Support;
using Xunit;

namespace RecallCommander.IntegrationTests.Integration;

/// <summary>
/// Workspace initializer + connection factory + source repository against a
/// real temporary SQLite database.
/// </summary>
public sealed class SourceRepositoryTests : IDisposable
{
    private readonly TestWorkspace _workspace = new();
    private readonly TestDataPaths _paths;

    public SourceRepositoryTests()
    {
        _paths = new TestDataPaths(_workspace.DataDirectory);
    }

    public void Dispose() => _workspace.Dispose();

    private SqliteQuestionSourceRepository CreateRepository() =>
        new(new SqliteConnectionFactory(_paths));

    private Task InitializeAsync() => new WorkspaceInitializer(_paths).InitializeAsync();

    [Fact]
    public async Task Initialize_creates_the_database_and_is_idempotent()
    {
        InitializationResult first = await new WorkspaceInitializer(_paths).InitializeAsync();
        InitializationResult second = await new WorkspaceInitializer(_paths).InitializeAsync();

        Assert.True(first.Created);
        Assert.False(second.Created);
        Assert.True(File.Exists(_paths.DatabasePath));
    }

    [Fact]
    public async Task Sources_roundtrip_through_sqlite()
    {
        await InitializeAsync();
        SqliteQuestionSourceRepository repository = CreateRepository();
        DateTimeOffset registeredAt = new DateTimeOffset(2026, 7, 16, 12, 30, 45, TimeSpan.Zero);

        await repository.AddAsync("/notes/csharp", registeredAt);
        await repository.AddAsync("/notes/physics", registeredAt.AddMinutes(5));

        IReadOnlyList<QuestionSource> sources = await repository.GetAllAsync();

        Assert.Equal(2, sources.Count);
        Assert.Equal("/notes/csharp", sources[0].DirectoryPath);
        Assert.Equal(registeredAt, sources[0].RegisteredAtUtc);
        Assert.Equal("/notes/physics", sources[1].DirectoryPath);
        Assert.True(sources[0].Id < sources[1].Id);
    }

    [Fact]
    public async Task Exists_matches_registered_paths_exactly()
    {
        await InitializeAsync();
        SqliteQuestionSourceRepository repository = CreateRepository();
        await repository.AddAsync("/notes/csharp", DateTimeOffset.UtcNow);

        Assert.True(await repository.ExistsAsync("/notes/csharp"));
        Assert.False(await repository.ExistsAsync("/notes/csharp/nested"));
        Assert.False(await repository.ExistsAsync("/other"));
    }

    [Fact]
    public async Task Data_persists_across_repository_instances()
    {
        await InitializeAsync();
        await CreateRepository().AddAsync("/notes/csharp", DateTimeOffset.UtcNow);

        SqliteQuestionSourceRepository freshRepository = CreateRepository();

        Assert.Single(await freshRepository.GetAllAsync());
    }

    [Fact]
    public async Task Uninitialized_workspace_is_rejected_with_a_clear_error()
    {
        SqliteQuestionSourceRepository repository = CreateRepository();

        await Assert.ThrowsAsync<WorkspaceNotInitializedException>(() => repository.GetAllAsync());
    }
}
