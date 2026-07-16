using Xunit;
using RecallCommander.Markdown.Writing;

namespace RecallCommander.Markdown.Tests;

public sealed class MarkdownArtifactBuilderTests
{
    private sealed record AssessmentFrontmatter(
        string Type,
        DateTimeOffset Created,
        string Title,
        int QuestionCount,
        string? Assessment = null);

    [Fact]
    public void Builds_document_matching_the_artifact_model_example()
    {
        var document = new MarkdownArtifactBuilder()
            .WithFrontmatter(new AssessmentFrontmatter(
                "assessment",
                new DateTimeOffset(2026, 7, 13, 19, 30, 0, TimeSpan.Zero),
                "C# Memory Management Assessment",
                3))
            .AppendHeading(1, "C# Memory Management Assessment")
            .AppendMarkdown("Answer the following questions in your own words.")
            .AppendThematicBreak()
            .AppendHeading(2, "Question 1")
            .AppendMarkdown("What is boxing in C#?")
            .Build();

        const string expected =
            """
            ---
            type: assessment
            created: 2026-07-13T19:30:00
            title: C# Memory Management Assessment
            question_count: 3
            ---

            # C# Memory Management Assessment

            Answer the following questions in your own words.

            ---

            ## Question 1

            What is boxing in C#?

            """;

        Assert.Equal(expected.ReplaceLineEndings("\n"), document);
    }

    [Fact]
    public void Frontmatter_omits_null_fields()
    {
        var document = new MarkdownArtifactBuilder()
            .WithFrontmatter(new AssessmentFrontmatter(
                "assessment",
                new DateTimeOffset(2026, 7, 13, 19, 30, 0, TimeSpan.Zero),
                "Title",
                1,
                Assessment: null))
            .AppendHeading(1, "Title")
            .Build();

        Assert.DoesNotContain("assessment:", document);
    }

    [Fact]
    public void Document_without_frontmatter_starts_with_the_body()
    {
        var document = new MarkdownArtifactBuilder()
            .AppendHeading(1, "Title")
            .Build();

        Assert.Equal("# Title\n", document);
    }

    [Fact]
    public void Multiline_markdown_blocks_are_preserved()
    {
        var document = new MarkdownArtifactBuilder()
            .AppendMarkdown("Explain the following:\n\n- generations\n- the large object heap")
            .Build();

        Assert.Equal("Explain the following:\n\n- generations\n- the large object heap\n", document);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Rejects_invalid_heading_levels(int level)
    {
        var builder = new MarkdownArtifactBuilder();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AppendHeading(level, "Title"));
    }
}
