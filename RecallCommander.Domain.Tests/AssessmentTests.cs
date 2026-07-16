using Xunit;
using RecallCommander.Domain;

namespace RecallCommander.Domain.Tests;

public sealed class AssessmentTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 7, 16, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Creates_assessment_snapshot()
    {
        var assessment = new Assessment(
            "C# Assessment",
            CreatedAt,
            [new AssessmentQuestion("What is boxing?")]);

        Assert.Equal("C# Assessment", assessment.Title);
        Assert.Equal(CreatedAt, assessment.CreatedAtUtc);
        Assert.Equal("What is boxing?", Assert.Single(assessment.Questions).Prompt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_empty_title(string title)
    {
        Assert.Throws<DomainException>(() =>
            new Assessment(title, CreatedAt, [new AssessmentQuestion("Prompt?")]));
    }

    [Fact]
    public void Rejects_empty_question_list()
    {
        Assert.Throws<DomainException>(() => new Assessment("Title", CreatedAt, []));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_empty_question_prompt(string prompt)
    {
        Assert.Throws<DomainException>(() => new AssessmentQuestion(prompt));
    }

    [Fact]
    public void Copies_only_the_prompt_from_a_discovered_question()
    {
        var question = new Question(
            QuestionType.Recall,
            "What is boxing?",
            "Boxing converts a value type into an object.",
            ["Boxing", "Value Types"]);

        var snapshot = AssessmentQuestion.FromQuestion(question);

        Assert.Equal("What is boxing?", snapshot.Prompt);
    }
}
