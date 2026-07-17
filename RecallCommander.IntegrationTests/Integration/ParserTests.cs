using Xunit;
using RecallCommander.Domain;
using RecallCommander.Infrastructure.FileSystem;
using RecallCommander.IntegrationTests.Support;
using RecallCommander.Markdown.Parsing;

namespace RecallCommander.IntegrationTests.Integration;

/// <summary>
/// Question Block parser fed with realistic files read from disk through the
/// real filesystem implementation.
/// </summary>
public sealed class ParserTests : IDisposable
{
    private readonly TestWorkspace _workspace = new();
    private readonly PhysicalFileSystem _fileSystem = new();
    private readonly QuestionBlockParser _parser = new();

    public void Dispose() => _workspace.Dispose();

    [Fact]
    public void Parses_all_question_types_from_a_realistic_notes_file()
    {
        var path = _workspace.WriteQuestionFile("csharp.md", SampleQuestions.CSharpFile());

        var result = _parser.Parse(_fileSystem.ReadAllText(path));

        Assert.Empty(result.Diagnostics);
        Assert.Equal(3, result.Questions.Count);
        Assert.Equal(
            [QuestionType.Recall, QuestionType.Explanation, QuestionType.Synthesis],
            result.Questions.Select(discovered => discovered.Question.Type));

        var recall = result.Questions[0].Question;
        Assert.Equal("What is boxing in C#?", recall.Prompt);
        Assert.Equal("Boxing converts a value type into an object on the managed heap.", recall.ReferenceAnswer);
        Assert.Equal(["Boxing", "Value Types"], recall.Concepts);

        var synthesis = result.Questions[2].Question;
        Assert.Null(synthesis.ReferenceAnswer);
    }

    [Fact]
    public void Reports_line_numbers_that_point_into_the_file()
    {
        var content = SampleQuestions.FileWithMalformedBlocks();
        var path = _workspace.WriteQuestionFile("mixed.md", content);
        var lines = content.Split('\n');

        var result = _parser.Parse(_fileSystem.ReadAllText(path));

        Assert.All(result.Diagnostics, diagnostic =>
        {
            Assert.InRange(diagnostic.LineNumber, 1, lines.Length);
            // Every diagnostic anchors to an rc-question opening line.
            Assert.StartsWith(":::rc-question", lines[diagnostic.LineNumber - 1]);
        });
    }
}
