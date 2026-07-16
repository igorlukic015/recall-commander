using RecallCommander.Domain;

namespace RecallCommander.Application.Assessments;

/// <summary>
/// Chooses which discovered questions go into an assessment. The MVP picks
/// randomly; future implementations can evolve into adaptive scheduling
/// without touching assessment creation.
/// </summary>
public interface IQuestionSelector
{
    /// <summary>
    /// Selects up to <paramref name="count"/> questions without duplicates.
    /// Returns all questions when fewer than <paramref name="count"/> exist.
    /// </summary>
    IReadOnlyList<Question> Select(IReadOnlyList<Question> questions, int count);
}
