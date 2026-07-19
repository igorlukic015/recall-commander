using RecallCommander.Contracts.Assessments;
using RecallCommander.Domain;

namespace RecallCommander.Application.Assessments;

/// <summary>
/// Uniform random selection without replacement (Fisher–Yates shuffle).
/// </summary>
public sealed class RandomQuestionSelector(Random random) : IQuestionSelector
{
    public RandomQuestionSelector()
        : this(Random.Shared)
    {
    }

    public IReadOnlyList<Question> Select(IReadOnlyList<Question> questions, int count)
    {
        ArgumentNullException.ThrowIfNull(questions);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        Question[] shuffled = questions.ToArray();
        for (int index = shuffled.Length - 1; index > 0; index--)
        {
            int swapIndex = random.Next(index + 1);
            (shuffled[index], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[index]);
        }

        return shuffled.Take(count).ToList();
    }
}
