using RecallCommander.Contracts.Reviews;
using RecallCommander.Domain;

namespace RecallCommander.Application.Reviews;

/// <summary>
/// A deterministic stand-in evaluator: fixed feedback for answered questions,
/// fixed feedback for unanswered ones. It exists so the review workflow runs
/// end-to-end without external services; real evaluators replace it behind
/// <see cref="IQuestionEvaluator"/> without touching the rest of the slice.
/// </summary>
public sealed class FakeQuestionEvaluator : IQuestionEvaluator
{
    public string Name => "fake";

    public Task<ReviewEvaluation> EvaluateAsync(
        string prompt,
        string answer,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNull(answer);

        return Task.FromResult(
            string.IsNullOrWhiteSpace(answer) ? UnansweredEvaluation() : AnsweredEvaluation());
    }

    private static ReviewEvaluation AnsweredEvaluation() => new(
        score: 8,
        UnderstandingLevel.Strong,
        "The answer demonstrates a good understanding.",
        strengths: ["The core idea is explained clearly."],
        missingInformation: ["A concrete example."],
        incorrectStatements: [],
        suggestions: ["Add an example to strengthen the answer."]);

    private static ReviewEvaluation UnansweredEvaluation() => new(
        score: 0,
        UnderstandingLevel.Poor,
        "The question was not answered.",
        strengths: [],
        missingInformation: ["An answer."],
        incorrectStatements: [],
        suggestions: ["Answer the question and create a new review."]);
}
