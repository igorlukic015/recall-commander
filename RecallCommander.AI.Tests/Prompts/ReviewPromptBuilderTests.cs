using RecallCommander.AI.Clients;
using RecallCommander.AI.Evaluation;
using RecallCommander.AI.Prompts;
using Xunit;

namespace RecallCommander.AI.Tests.Prompts;

public sealed class ReviewPromptBuilderTests
{
    private readonly ReviewPromptBuilder _builder = new(new PromptLoader());

    [Fact]
    public void Loads_the_review_system_prompt()
    {
        AiRequest request = _builder.Build("What is boxing?", "An answer.");

        Assert.Contains("\"score\"", request.SystemPrompt);
        Assert.Contains("\"level\"", request.SystemPrompt);
        Assert.Contains("Poor, Weak, Partial, Good, Strong, Excellent", request.SystemPrompt);
    }

    [Fact]
    public void Injects_the_question_into_the_user_prompt()
    {
        AiRequest request = _builder.Build("What is boxing in C#?", "An answer.");

        Assert.Contains("What is boxing in C#?", request.UserPrompt);
    }

    [Fact]
    public void Injects_the_answer_into_the_user_prompt()
    {
        AiRequest request = _builder.Build("What is boxing?", "Boxing wraps a value type in an object.");

        Assert.Contains("Boxing wraps a value type in an object.", request.UserPrompt);
    }

    [Fact]
    public void Leaves_no_placeholders_behind()
    {
        AiRequest request = _builder.Build("What is boxing?", "An answer.");

        Assert.DoesNotContain("{{", request.UserPrompt);
        Assert.DoesNotContain("{{", request.SystemPrompt);
    }

    [Fact]
    public void An_empty_answer_produces_an_empty_answer_section()
    {
        AiRequest request = _builder.Build("What is boxing?", "");

        Assert.Contains("# Answer", request.UserPrompt);
        Assert.DoesNotContain("{{answer}}", request.UserPrompt);
    }

    [Fact]
    public void Preserves_multiline_markdown_answers()
    {
        string answer = "The GC works in phases:\n\n- mark\n- sweep";

        AiRequest request = _builder.Build("Explain garbage collection.", answer);

        Assert.Contains(answer, request.UserPrompt);
    }

    [Fact]
    public void The_loader_reports_a_missing_prompt()
    {
        AiException exception = Assert.Throws<AiException>(() => new PromptLoader().Load("Review/Nope.md"));

        Assert.Contains("Review/Nope.md", exception.Message);
    }
}
