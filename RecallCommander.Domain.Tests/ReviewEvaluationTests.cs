using RecallCommander.Domain;
using Xunit;

namespace RecallCommander.Domain.Tests;

public sealed class ReviewEvaluationTests
{
    private static ReviewEvaluation Create(
        int score = 8,
        UnderstandingLevel understanding = UnderstandingLevel.Strong,
        string summary = "A good answer.") => new(
        score,
        understanding,
        summary,
        strengths: ["Clear explanation."],
        missingInformation: ["An example."],
        incorrectStatements: [],
        suggestions: ["Add an example."]);

    [Fact]
    public void Creates_evaluation()
    {
        ReviewEvaluation evaluation = Create();

        Assert.Equal(8, evaluation.Score);
        Assert.Equal(UnderstandingLevel.Strong, evaluation.Understanding);
        Assert.Equal("A good answer.", evaluation.Summary);
        Assert.Equal(["Clear explanation."], evaluation.Strengths);
        Assert.Equal(["An example."], evaluation.MissingInformation);
        Assert.Empty(evaluation.IncorrectStatements);
        Assert.Equal(["Add an example."], evaluation.Suggestions);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void Accepts_boundary_scores(int score)
    {
        Assert.Equal(score, Create(score).Score);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Rejects_score_outside_range(int score)
    {
        Assert.Throws<DomainException>(() => Create(score));
    }

    [Fact]
    public void Rejects_undefined_understanding_level()
    {
        Assert.Throws<DomainException>(() => Create(understanding: (UnderstandingLevel)99));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_empty_summary(string summary)
    {
        Assert.Throws<DomainException>(() => Create(summary: summary));
    }

    [Fact]
    public void Rejects_null_feedback_list()
    {
        Assert.Throws<DomainException>(() => new ReviewEvaluation(
            8, UnderstandingLevel.Strong, "Summary.", null!, [], [], []));
    }

    [Fact]
    public void Rejects_empty_feedback_list_entries()
    {
        Assert.Throws<DomainException>(() => new ReviewEvaluation(
            8, UnderstandingLevel.Strong, "Summary.", ["Fine.", "   "], [], [], []));
    }

    [Fact]
    public void Trims_summary_and_feedback_entries()
    {
        ReviewEvaluation evaluation = new ReviewEvaluation(
            8, UnderstandingLevel.Strong, "  Summary.  ", ["  Clear.  "], [], [], []);

        Assert.Equal("Summary.", evaluation.Summary);
        Assert.Equal(["Clear."], evaluation.Strengths);
    }

    [Fact]
    public void Feedback_lists_are_snapshots_of_the_input()
    {
        List<string> strengths = ["Clear."];
        ReviewEvaluation evaluation = new ReviewEvaluation(
            8, UnderstandingLevel.Strong, "Summary.", strengths, [], [], []);

        strengths.Add("Added later.");

        Assert.Equal(["Clear."], evaluation.Strengths);
    }
}
