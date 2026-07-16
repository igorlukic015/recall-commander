using Xunit;
using RecallCommander.Domain;

namespace RecallCommander.Domain.Tests;

public sealed class QuestionTests
{
    [Fact]
    public void Creates_question_with_all_fields()
    {
        var question = new Question(
            QuestionType.Recall,
            "What is boxing?",
            "Boxing converts a value type into an object.",
            ["Boxing", "Value Types"]);

        Assert.Equal(QuestionType.Recall, question.Type);
        Assert.Equal("What is boxing?", question.Prompt);
        Assert.Equal("Boxing converts a value type into an object.", question.ReferenceAnswer);
        Assert.Equal(["Boxing", "Value Types"], question.Concepts);
    }

    [Fact]
    public void Answer_and_concepts_are_optional()
    {
        var question = new Question(QuestionType.Synthesis, "How do concepts connect?");

        Assert.Null(question.ReferenceAnswer);
        Assert.Empty(question.Concepts);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_empty_prompt(string prompt)
    {
        Assert.Throws<DomainException>(() => new Question(QuestionType.Recall, prompt));
    }

    [Fact]
    public void Rejects_whitespace_reference_answer()
    {
        Assert.Throws<DomainException>(() =>
            new Question(QuestionType.Recall, "What is boxing?", "   "));
    }

    [Fact]
    public void Rejects_empty_concepts()
    {
        Assert.Throws<DomainException>(() =>
            new Question(QuestionType.Recall, "What is boxing?", concepts: ["Boxing", " "]));
    }

    [Fact]
    public void Trims_prompt_answer_and_concepts()
    {
        var question = new Question(
            QuestionType.Recall,
            "  What is boxing?  ",
            "  An answer.  ",
            ["  Boxing  "]);

        Assert.Equal("What is boxing?", question.Prompt);
        Assert.Equal("An answer.", question.ReferenceAnswer);
        Assert.Equal(["Boxing"], question.Concepts);
    }

    [Fact]
    public void Removes_duplicate_concepts()
    {
        var question = new Question(
            QuestionType.Recall,
            "What is boxing?",
            concepts: ["Boxing", "Boxing", "Heap"]);

        Assert.Equal(["Boxing", "Heap"], question.Concepts);
    }
}
