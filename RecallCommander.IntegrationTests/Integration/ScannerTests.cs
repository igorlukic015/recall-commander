using Xunit;
using RecallCommander.Application.Scanning;
using RecallCommander.Infrastructure.FileSystem;
using RecallCommander.IntegrationTests.Support;
using RecallCommander.Markdown.Parsing;

namespace RecallCommander.IntegrationTests.Integration;

/// <summary>
/// Real filesystem + real Question Block parser + scan orchestration.
/// </summary>
public sealed class ScannerTests : IDisposable
{
    private readonly TestWorkspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    private Task<ScanReport> ScanAsync(params string[] sourceDirectories)
    {
        var service = new ScanService(
            new StubQuestionSourceRepository(sourceDirectories),
            new PhysicalFileSystem(),
            new QuestionBlockParser());
        return service.ScanAsync();
    }

    [Fact]
    public async Task Discovers_questions_recursively_in_nested_directories()
    {
        _workspace.WriteQuestionFile("csharp.md", SampleQuestions.CSharpFile());
        _workspace.WriteQuestionFile("nested/dotnet.md", SampleQuestions.DotNetFile());
        _workspace.WriteQuestionFile("nested/deeper/more.md", SampleQuestions.Recall("Deep question?"));

        var report = await ScanAsync(_workspace.QuestionsDirectory);

        Assert.Empty(report.Warnings);
        Assert.Equal(6, report.TotalQuestions);
        Assert.Contains(report.Files, file => file.DisplayPath == Path.Combine("nested", "deeper", "more.md"));
    }

    [Fact]
    public async Task Scans_multiple_source_folders()
    {
        _workspace.WriteQuestionFile("csharp.md", SampleQuestions.CSharpFile());
        var secondSource = _workspace.CreateSourceDirectory("MoreQuestions");
        _workspace.WriteFile(secondSource, "physics.md", SampleQuestions.Recall("What is inertia?"));

        var report = await ScanAsync(_workspace.QuestionsDirectory, secondSource);

        Assert.Equal(4, report.TotalQuestions);
        Assert.Contains(report.Files, file => file.DisplayPath == "physics.md");
    }

    [Fact]
    public async Task Ignores_ordinary_markdown_outside_question_blocks()
    {
        _workspace.WriteQuestionFile("notes.md", SampleQuestions.PlainNotesFile());

        var report = await ScanAsync(_workspace.QuestionsDirectory);

        Assert.Empty(report.Warnings);
        Assert.Equal(0, report.TotalQuestions);
    }

    [Fact]
    public async Task Ignores_non_markdown_files()
    {
        _workspace.WriteQuestionFile("questions.txt", SampleQuestions.Recall("Hidden in a txt file?"));

        var report = await ScanAsync(_workspace.QuestionsDirectory);

        Assert.Empty(report.Files);
        Assert.Equal(0, report.TotalQuestions);
    }

    [Fact]
    public async Task Skips_malformed_blocks_and_reports_errors_while_keeping_valid_questions()
    {
        _workspace.WriteQuestionFile("mixed.md", SampleQuestions.FileWithMalformedBlocks());

        var report = await ScanAsync(_workspace.QuestionsDirectory);

        Assert.Equal(2, report.TotalQuestions);
        Assert.Equal(3, report.Warnings.Count);
        Assert.All(report.Warnings, warning =>
        {
            Assert.Equal("mixed.md", warning.DisplayPath);
            Assert.NotNull(warning.LineNumber);
            Assert.True(warning.LineNumber > 0);
        });
        Assert.Contains(report.Warnings, warning => warning.Message.Contains("'type'"));
        Assert.Contains(report.Warnings, warning => warning.Message.Contains("Missing rc-prompt"));
        Assert.Contains(report.Warnings, warning => warning.Message.Contains("Quiz"));
    }

    [Fact]
    public async Task Continues_scanning_after_a_file_full_of_failures()
    {
        _workspace.WriteQuestionFile("a-broken.md", SampleQuestions.MissingPromptBlock());
        _workspace.WriteQuestionFile("b-valid.md", SampleQuestions.Recall("Still discovered?"));

        var report = await ScanAsync(_workspace.QuestionsDirectory);

        Assert.Single(report.Warnings);
        Assert.Equal(1, report.TotalQuestions);
    }

    [Fact]
    public async Task Reports_missing_source_directory_and_scans_the_rest()
    {
        _workspace.WriteQuestionFile("csharp.md", SampleQuestions.Recall("What is boxing?"));
        var missing = Path.Combine(_workspace.Root, "does-not-exist");

        var report = await ScanAsync(missing, _workspace.QuestionsDirectory);

        var warning = Assert.Single(report.Warnings);
        Assert.Equal(missing, warning.DisplayPath);
        Assert.Equal(1, report.TotalQuestions);
    }
}
