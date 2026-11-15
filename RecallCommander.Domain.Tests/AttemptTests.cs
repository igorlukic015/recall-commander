using RecallCommander.Domain;
using Xunit;

namespace RecallCommander.Domain.Tests;

public sealed class AttemptTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 7, 17, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Creates_attempt_snapshot()
    {
        Attempt attempt = new Attempt(
            "C# Assessment",
            CreatedAt,
            [new AttemptQuestion("What is boxing?", "Boxing wraps a value type in an object.")]);

        Assert.Equal("C# Assessment", attempt.Title);
        Assert.Equal(CreatedAt, attempt.CreatedAtUtc);

        AttemptQuestion question = Assert.Single(attempt.Questions);
        Assert.Equal("What is boxing?", question.Prompt);
        Assert.Equal("Boxing wraps a value type in an object.", question.Answer);
        Assert.True(question.IsAnswered);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_empty_title(string title)
    {
        Assert.Throws<DomainException>(() =>
            new Attempt(title, CreatedAt, [new AttemptQuestion("Prompt?", "Answer.")]));
    }

    [Fact]
    public void Rejects_empty_question_list()
    {
        Assert.Throws<DomainException>(() => new Attempt("Title", CreatedAt, []));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_empty_question_prompt(string prompt)
    {
        Assert.Throws<DomainException>(() => new AttemptQuestion(prompt, "Answer."));
    }

    [Fact]
    public void An_empty_answer_is_valid_and_means_unanswered()
    {
        AttemptQuestion question = new AttemptQuestion("What is boxing?", "");

        Assert.Equal("", question.Answer);
        Assert.False(question.IsAnswered);
    }

    [Fact]
    public void A_whitespace_answer_normalizes_to_unanswered()
    {
        AttemptQuestion question = new AttemptQuestion("What is boxing?", "   \n   ");

        Assert.Equal("", question.Answer);
        Assert.False(question.IsAnswered);
    }

    [Fact]
    public void Rejects_null_answer()
    {
        Assert.Throws<DomainException>(() => new AttemptQuestion("Prompt?", null!));
    }

    [Fact]
    public void Trims_prompt_and_answer()
    {
        AttemptQuestion question = new AttemptQuestion("  What is boxing?  ", "  An answer.  ");

        Assert.Equal("What is boxing?", question.Prompt);
        Assert.Equal("An answer.", question.Answer);
    }
}
