using RecallCommander.Contracts.Sources;
using RecallCommander.Domain;

namespace RecallCommander.Application.Tests.Fakes;

public sealed class InMemoryQuestionSourceRepository : IQuestionSourceRepository
{
    private readonly List<QuestionSource> _sources = [];

    public Task<QuestionSource> AddAsync(
        string directoryPath,
        DateTimeOffset registeredAtUtc,
        CancellationToken cancellationToken = default)
    {
        QuestionSource source = new QuestionSource(_sources.Count + 1, directoryPath, registeredAtUtc);
        _sources.Add(source);
        return Task.FromResult(source);
    }

    public Task<IReadOnlyList<QuestionSource>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<QuestionSource>>(_sources.ToList());

    public Task<bool> ExistsAsync(string directoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sources.Any(source => source.DirectoryPath == directoryPath));

    public Task<bool> RemoveAsync(string directoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sources.RemoveAll(source => source.DirectoryPath == directoryPath) > 0);
}
