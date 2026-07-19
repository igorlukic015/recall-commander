using RecallCommander.Domain;
using Xunit;

namespace RecallCommander.Domain.Tests;

public sealed class ReviewTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 7, 19, 20, 0, 0, TimeSpan.Zero);

    private static ReviewEvaluation Evaluation() => new(
        8,
        UnderstandingLevel.Strong,
        "A good answer.",
        strengths: ["Clear explanation."],
        missingInformation: [],
        incorrectStatements: [],
        suggestions: []);

    private static QuestionReview SampleQuestionReview() =>
        new("What is boxing?", "Boxing wraps a value type in an object.", Evaluation());

    [Fact]
    public void Creates_review_snapshot()
    {
        Review review = new Review(
            "Review - C# Assessment",
            CreatedAt,
            "1 of 1 questions were answered.",
            [SampleQuestionReview()],
            "assessment-2026-07-19-001",
            "fake");

        Assert.Equal("Review - C# Assessment", review.Title);
        Assert.Equal(CreatedAt, review.CreatedAtUtc);
        Assert.Equal("assessment-2026-07-19-001", review.AttemptId);
        Assert.Equal("fake", review.Evaluator);
        Assert.Equal("1 of 1 questions were answered.", review.OverallSummary);

        QuestionReview question = Assert.Single(review.QuestionReviews);
        Assert.Equal("What is boxing?", question.Prompt);
        Assert.Equal("Boxing wraps a value type in an object.", question.Answer);
        Assert.Equal(8, question.Evaluation.Score);
    }

    [Fact]
    public void Attempt_id_and_evaluator_are_optional()
    {
        Review review = new Review("Title", CreatedAt, "Summary.", [SampleQuestionReview()]);

        Assert.Null(review.AttemptId);
        Assert.Null(review.Evaluator);
    }

    [Fact]
    public void Whitespace_attempt_id_and_evaluator_normalize_to_null()
    {
        Review review = new Review(
            "Title", CreatedAt, "Summary.", [SampleQuestionReview()], attemptId: "   ", evaluator: "   ");

        Assert.Null(review.AttemptId);
        Assert.Null(review.Evaluator);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_empty_title(string title)
    {
        Assert.Throws<DomainException>(() =>
            new Review(title, CreatedAt, "Summary.", [SampleQuestionReview()]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_empty_overall_summary(string summary)
    {
        Assert.Throws<DomainException>(() =>
            new Review("Title", CreatedAt, summary, [SampleQuestionReview()]));
    }

    [Fact]
    public void Rejects_empty_question_review_list()
    {
        Assert.Throws<DomainException>(() => new Review("Title", CreatedAt, "Summary.", []));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_empty_question_prompt(string prompt)
    {
        Assert.Throws<DomainException>(() => new QuestionReview(prompt, "Answer.", Evaluation()));
    }

    [Fact]
    public void Rejects_null_answer()
    {
        Assert.Throws<DomainException>(() => new QuestionReview("Prompt?", null!, Evaluation()));
    }

    [Fact]
    public void Rejects_missing_evaluation()
    {
        Assert.Throws<DomainException>(() => new QuestionReview("Prompt?", "Answer.", null!));
    }

    [Fact]
    public void An_empty_answer_is_valid_and_means_unanswered()
    {
        QuestionReview question = new QuestionReview("Prompt?", "", Evaluation());

        Assert.Equal("", question.Answer);
    }

    [Fact]
    public void Trims_title_prompt_and_answer()
    {
        Review review = new Review(
            "  Title  ",
            CreatedAt,
            "  Summary.  ",
            [new QuestionReview("  Prompt?  ", "  Answer.  ", Evaluation())]);

        Assert.Equal("Title", review.Title);
        Assert.Equal("Summary.", review.OverallSummary);
        Assert.Equal("Prompt?", review.QuestionReviews[0].Prompt);
        Assert.Equal("Answer.", review.QuestionReviews[0].Answer);
    }
}
