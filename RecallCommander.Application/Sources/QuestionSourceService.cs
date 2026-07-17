using RecallCommander.Contracts.FileSystem;
using RecallCommander.Contracts.Sources;
using RecallCommander.Domain;

namespace RecallCommander.Application.Sources;

/// <summary>
/// Registers and lists question sources.
/// </summary>
public sealed class QuestionSourceService(
    IQuestionSourceRepository repository,
    IFileSystem fileSystem,
    TimeProvider timeProvider)
{
    public async Task<AddSourceResult> AddAsync(string path, CancellationToken cancellationToken = default)
    {
        var directoryPath = fileSystem.NormalizePath(path);

        if (!fileSystem.DirectoryExists(directoryPath))
        {
            return new AddSourceResult(AddSourceStatus.DirectoryNotFound, directoryPath, Source: null);
        }

        if (await repository.ExistsAsync(directoryPath, cancellationToken))
        {
            return new AddSourceResult(AddSourceStatus.AlreadyRegistered, directoryPath, Source: null);
        }

        var source = await repository.AddAsync(directoryPath, timeProvider.GetUtcNow(), cancellationToken);
        return new AddSourceResult(AddSourceStatus.Added, directoryPath, source);
    }

    public Task<IReadOnlyList<QuestionSource>> ListAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);
}
