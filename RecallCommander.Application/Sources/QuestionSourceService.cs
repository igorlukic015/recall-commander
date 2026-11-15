using RecallCommander.Contracts.FileSystem;
using RecallCommander.Contracts.Sources;
using RecallCommander.Domain;

namespace RecallCommander.Application.Sources;

/// <summary>
/// Registers, removes and lists question sources.
/// </summary>
public sealed class QuestionSourceService(
    IQuestionSourceRepository repository,
    IFileSystem fileSystem,
    TimeProvider timeProvider)
{
    public async Task<AddSourceResult> AddAsync(string path, CancellationToken cancellationToken = default)
    {
        string directoryPath = fileSystem.NormalizePath(path);

        if (!fileSystem.DirectoryExists(directoryPath))
        {
            return new AddSourceResult(AddSourceStatus.DirectoryNotFound, directoryPath, Source: null);
        }

        if (await repository.ExistsAsync(directoryPath, cancellationToken))
        {
            return new AddSourceResult(AddSourceStatus.AlreadyRegistered, directoryPath, Source: null);
        }

        QuestionSource source = await repository.AddAsync(directoryPath, timeProvider.GetUtcNow(), cancellationToken);
        return new AddSourceResult(AddSourceStatus.Added, directoryPath, source);
    }

    /// <summary>
    /// Unregisters a source. The directory does not need to exist — removing
    /// a stale registration is the main use case. The directory's contents
    /// are never touched; only the registration is removed.
    /// </summary>
    public async Task<RemoveSourceResult> RemoveAsync(string path, CancellationToken cancellationToken = default)
    {
        string directoryPath = fileSystem.NormalizePath(path);

        bool removed = await repository.RemoveAsync(directoryPath, cancellationToken);

        return new RemoveSourceResult(
            removed ? RemoveSourceStatus.Removed : RemoveSourceStatus.NotRegistered,
            directoryPath);
    }

    public Task<IReadOnlyList<QuestionSource>> ListAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);
}
