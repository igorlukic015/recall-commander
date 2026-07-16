using Xunit;
using RecallCommander.Domain;
using RecallCommander.Markdown.Parsing;

namespace RecallCommander.Markdown.Tests;

public sealed class QuestionBlockParserTests
{
    private readonly QuestionBlockParser _parser = new();

    [Fact]
    public void Parses_complete_question_block()
    {
        const string markdown =
            """
            :::rc-question

            type: Recall

            concepts:
            - Boxing
            - Value Types

            :::rc-prompt

            What is boxing in C#?

            :::

            :::rc-answer

            Boxing converts a value type into an object.

            :::

            :::
            """;

        var result = _parser.Parse(markdown);

        Assert.Empty(result.Diagnostics);
        var discovered = Assert.Single(result.Questions);
        Assert.Equal(1, discovered.LineNumber);

        var question = discovered.Question;
        Assert.Equal(QuestionType.Recall, question.Type);
        Assert.Equal("What is boxing in C#?", question.Prompt);
        Assert.Equal("Boxing converts a value type into an object.", question.ReferenceAnswer);
        Assert.Equal(["Boxing", "Value Types"], question.Concepts);
    }

    [Fact]
    public void Answer_block_is_optional()
    {
        const string markdown =
            """
            :::rc-question

            type: Synthesis

            :::rc-prompt

            How do allocation patterns affect performance?

            :::

            :::
            """;

        var result = _parser.Parse(markdown);

        Assert.Empty(result.Diagnostics);
        var question = Assert.Single(result.Questions).Question;
        Assert.Equal(QuestionType.Synthesis, question.Type);
        Assert.Null(question.ReferenceAnswer);
    }

    [Fact]
    public void Concepts_are_optional_and_default_to_empty()
    {
        var result = _parser.Parse(QuestionBlock(type: "Explanation", prompt: "Explain GC."));

        Assert.Empty(result.Diagnostics);
        Assert.Empty(Assert.Single(result.Questions).Question.Concepts);
    }

    [Fact]
    public void Everything_outside_question_blocks_is_ignored()
    {
        const string markdown =
            """
            # My Notes

            Boxing is important.

            ---

            :::rc-question

            type: Recall

            :::rc-prompt

            What is boxing?

            :::

            :::

            More notes down here.
            """;

        var result = _parser.Parse(markdown);

        Assert.Empty(result.Diagnostics);
        Assert.Single(result.Questions);
    }

    [Fact]
    public void Parses_multiple_question_blocks_in_one_document()
    {
        var markdown =
            QuestionBlock(type: "Recall", prompt: "Question one?") +
            "\n\nSome prose between questions.\n\n" +
            QuestionBlock(type: "Explanation", prompt: "Question two?");

        var result = _parser.Parse(markdown);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(2, result.Questions.Count);
        Assert.Equal(QuestionType.Recall, result.Questions[0].Question.Type);
        Assert.Equal(QuestionType.Explanation, result.Questions[1].Question.Type);
    }

    [Fact]
    public void Prompt_preserves_multiline_markdown_content()
    {
        const string markdown =
            """
            :::rc-question

            type: Explanation

            :::rc-prompt

            Explain garbage collection.

            Your answer should cover:

            - generations
            - the large object heap

            :::

            :::
            """;

        var result = _parser.Parse(markdown);

        var prompt = Assert.Single(result.Questions).Question.Prompt;
        Assert.Contains("Explain garbage collection.", prompt);
        Assert.Contains("- generations", prompt);
        Assert.Contains("- the large object heap", prompt);
    }

    [Fact]
    public void Code_fences_inside_prompt_do_not_terminate_the_block()
    {
        const string markdown =
            """
            :::rc-question

            type: Recall

            :::rc-prompt

            What does this print?

            ```csharp
            var text = ":::";
            Console.WriteLine(text);
            ```

            :::

            :::
            """;

        var result = _parser.Parse(markdown);

        Assert.Empty(result.Diagnostics);
        var prompt = Assert.Single(result.Questions).Question.Prompt;
        Assert.Contains("var text = \":::\";", prompt);
    }

    [Fact]
    public void Missing_type_reports_warning_and_skips_question()
    {
        const string markdown =
            """
            :::rc-question

            :::rc-prompt

            What is boxing?

            :::

            :::
            """;

        var result = _parser.Parse(markdown);

        Assert.Empty(result.Questions);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(1, diagnostic.LineNumber);
        Assert.Contains("'type'", diagnostic.Message);
    }

    [Fact]
    public void Unknown_type_reports_warning_and_skips_question()
    {
        var result = _parser.Parse(QuestionBlock(type: "Trivia", prompt: "What is boxing?"));

        Assert.Empty(result.Questions);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Contains("Trivia", diagnostic.Message);
        Assert.Contains("Recall", diagnostic.Message);
    }

    [Fact]
    public void Missing_prompt_reports_warning_and_skips_question()
    {
        const string markdown =
            """
            :::rc-question

            type: Recall

            :::

            Unrelated paragraph.
            """;

        var result = _parser.Parse(markdown);

        Assert.Empty(result.Questions);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Contains("Missing rc-prompt", diagnostic.Message);
    }

    [Fact]
    public void Empty_prompt_reports_warning_and_skips_question()
    {
        const string markdown =
            """
            :::rc-question

            type: Recall

            :::rc-prompt

            :::

            :::
            """;

        var result = _parser.Parse(markdown);

        Assert.Empty(result.Questions);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Contains("empty", diagnostic.Message);
    }

    [Fact]
    public void Invalid_metadata_yaml_reports_warning_and_skips_question()
    {
        const string markdown =
            """
            :::rc-question

            type: [unclosed

            :::rc-prompt

            What is boxing?

            :::

            :::
            """;

        var result = _parser.Parse(markdown);

        Assert.Empty(result.Questions);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Contains("Invalid metadata", diagnostic.Message);
    }

    [Fact]
    public void Duplicate_prompt_reports_warning_and_skips_question()
    {
        const string markdown =
            """
            :::rc-question

            type: Recall

            :::rc-prompt

            First prompt.

            :::

            :::rc-prompt

            Second prompt.

            :::

            :::
            """;

        var result = _parser.Parse(markdown);

        Assert.Empty(result.Questions);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Contains("Duplicate rc-prompt", diagnostic.Message);
    }

    [Fact]
    public void Scanning_continues_after_an_invalid_block()
    {
        var markdown =
            QuestionBlock(type: "Bogus", prompt: "Broken question?") +
            "\n\n" +
            QuestionBlock(type: "Recall", prompt: "Valid question?");

        var result = _parser.Parse(markdown);

        Assert.Single(result.Diagnostics);
        var question = Assert.Single(result.Questions).Question;
        Assert.Equal("Valid question?", question.Prompt);
    }

    [Fact]
    public void Diagnostics_carry_the_line_number_of_the_failing_block()
    {
        var markdown =
            "# Heading\n\nSome prose.\n\n" +                          // lines 1-4
            QuestionBlock(type: "Recall", prompt: "Fine?") + "\n\n" + // starts line 5
            """
            :::rc-question

            type: Recall

            :::
            """;                                                      // starts line 17

        var result = _parser.Parse(markdown);

        Assert.Equal(5, Assert.Single(result.Questions).LineNumber);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(17, diagnostic.LineNumber);
    }

    [Fact]
    public void Unknown_metadata_fields_are_ignored_for_forward_compatibility()
    {
        const string markdown =
            """
            :::rc-question

            type: Recall

            difficulty: hard

            :::rc-prompt

            What is boxing?

            :::

            :::
            """;

        var result = _parser.Parse(markdown);

        Assert.Empty(result.Diagnostics);
        Assert.Single(result.Questions);
    }

    [Fact]
    public void Unknown_nested_containers_are_ignored_for_forward_compatibility()
    {
        const string markdown =
            """
            :::rc-question

            type: Recall

            :::rc-prompt

            What is boxing?

            :::

            :::rc-hint

            Think about the heap.

            :::

            :::
            """;

        var result = _parser.Parse(markdown);

        Assert.Empty(result.Diagnostics);
        Assert.Single(result.Questions);
    }

    [Fact]
    public void Prompt_block_outside_question_block_reports_warning()
    {
        const string markdown =
            """
            :::rc-prompt

            A stray prompt.

            :::
            """;

        var result = _parser.Parse(markdown);

        Assert.Empty(result.Questions);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Contains("outside", diagnostic.Message);
    }

    [Fact]
    public void Empty_answer_block_is_treated_as_no_answer()
    {
        const string markdown =
            """
            :::rc-question

            type: Recall

            :::rc-prompt

            What is boxing?

            :::

            :::rc-answer

            :::

            :::
            """;

        var result = _parser.Parse(markdown);

        Assert.Empty(result.Diagnostics);
        Assert.Null(Assert.Single(result.Questions).Question.ReferenceAnswer);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n\n   ")]
    [InlineData("# Just notes\n\nNo questions here.")]
    public void Documents_without_question_blocks_yield_nothing(string markdown)
    {
        var result = _parser.Parse(markdown);

        Assert.Empty(result.Questions);
        Assert.Empty(result.Diagnostics);
    }

    private static string QuestionBlock(string type, string prompt) =>
        $"""
        :::rc-question

        type: {type}

        :::rc-prompt

        {prompt}

        :::

        :::
        """;
}
