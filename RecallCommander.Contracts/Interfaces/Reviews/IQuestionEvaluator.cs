using RecallCommander.Domain;

namespace RecallCommander.Contracts.Reviews;

/// <summary>
/// Evaluates one question and answer pair. How the evaluation is produced is
/// an implementation detail behind this boundary — a deterministic stub today,
/// external evaluation services later.
/// </summary>
public interface IQuestionEvaluator
{
    /// <summary>
    /// Identity of this evaluator, recorded in review artifacts
    /// (e.g. "fake", later a model name).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Evaluates the user's answer to a question. An empty answer means the
    /// question was left unanswered.
    /// </summary>
    Task<ReviewEvaluation> EvaluateAsync(
        string prompt,
        string answer,
        CancellationToken cancellationToken = default);
}
