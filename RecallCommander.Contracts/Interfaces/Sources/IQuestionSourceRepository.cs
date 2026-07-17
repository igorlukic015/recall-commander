using RecallCommander.Domain;

namespace RecallCommander.Contracts.Sources;

/// <summary>
/// Persistence for registered question sources. Only source metadata is stored;
/// questions themselves are never persisted.
/// </summary>
public interface IQuestionSourceRepository
{
    Task<QuestionSource> AddAsync(string directoryPath, DateTimeOffset registeredAtUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QuestionSource>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string directoryPath, CancellationToken cancellationToken = default);
}
