using RecallCommander.Application.Reviews;
using RecallCommander.Domain;
using Xunit;

namespace RecallCommander.Application.Tests.Reviews;

public sealed class FakeQuestionEvaluatorTests
{
    private readonly FakeQuestionEvaluator _evaluator = new();

    [Fact]
    public void Has_a_name_for_review_artifacts()
    {
        Assert.Equal("fake", _evaluator.Name);
    }

    [Fact]
    public async Task Returns_a_fixed_evaluation_for_an_answered_question()
    {
        ReviewEvaluation evaluation = await _evaluator.EvaluateAsync(
            "What is boxing?",
            "Boxing wraps a value type in an object.");

        Assert.Equal(8, evaluation.Score);
        Assert.Equal(UnderstandingLevel.Strong, evaluation.Understanding);
        Assert.Equal("The answer demonstrates a good understanding.", evaluation.Summary);
        Assert.NotEmpty(evaluation.Strengths);
        Assert.NotEmpty(evaluation.MissingInformation);
        Assert.Empty(evaluation.IncorrectStatements);
        Assert.NotEmpty(evaluation.Suggestions);
    }

    [Fact]
    public async Task Returns_a_poor_evaluation_for_an_unanswered_question()
    {
        ReviewEvaluation evaluation = await _evaluator.EvaluateAsync("What is boxing?", "");

        Assert.Equal(0, evaluation.Score);
        Assert.Equal(UnderstandingLevel.Poor, evaluation.Understanding);
        Assert.Equal("The question was not answered.", evaluation.Summary);
        Assert.Empty(evaluation.Strengths);
    }

    [Fact]
    public async Task Is_deterministic()
    {
        ReviewEvaluation first = await _evaluator.EvaluateAsync("Prompt?", "An answer.");
        ReviewEvaluation second = await _evaluator.EvaluateAsync("Prompt?", "An answer.");

        Assert.Equal(first.Score, second.Score);
        Assert.Equal(first.Understanding, second.Understanding);
        Assert.Equal(first.Summary, second.Summary);
        Assert.Equal(first.Strengths, second.Strengths);
        Assert.Equal(first.MissingInformation, second.MissingInformation);
        Assert.Equal(first.IncorrectStatements, second.IncorrectStatements);
        Assert.Equal(first.Suggestions, second.Suggestions);
    }

    [Fact]
    public async Task Rejects_an_empty_prompt()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _evaluator.EvaluateAsync("   ", "An answer."));
    }
}
