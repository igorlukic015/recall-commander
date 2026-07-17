using RecallCommander.IntegrationTests.Support;
using Xunit;

namespace RecallCommander.IntegrationTests.EndToEnd;

/// <summary>
/// Real user workflows executed through the CLI entry point:
/// rc init → rc source add → rc source list → rc scan → rc assessment create.
/// Only the console and the workspace locations are substituted; commands,
/// services, parser, SQLite and the filesystem are all production code.
/// </summary>
public sealed class AssessmentWorkflowTests : IDisposable
{
    private readonly TestWorkspace _workspace = new();
    private readonly CliRunner _cli;

    public AssessmentWorkflowTests()
    {
        _cli = new CliRunner(_workspace);
    }

    public void Dispose() => _workspace.Dispose();

    [Fact]
    public async Task Complete_workflow_from_question_source_to_assessment_file()
    {
        // Arrange: a realistic question source, including a malformed block.
        _workspace.WriteQuestionFile("csharp.md", SampleQuestions.CSharpFile());
        _workspace.WriteQuestionFile("nested/dotnet.md", SampleQuestions.DotNetFile());
        _workspace.WriteQuestionFile("broken.md", SampleQuestions.MissingPromptBlock());

        // Act + Assert: each step of the workflow succeeds.
        CliResult init = await _cli.RunAsync("init");
        Assert.Equal(0, init.ExitCode);
        Assert.Contains("Initialized", init.Output);

        CliResult add = await _cli.RunAsync("source", "add", _workspace.QuestionsDirectory);
        Assert.Equal(0, add.ExitCode);
        Assert.Contains("Registered question source", add.Output);

        CliResult list = await _cli.RunAsync("source", "list");
        Assert.Equal(0, list.ExitCode);
        Assert.Contains("Questions", list.Output);

        CliResult scan = await _cli.RunAsync("scan");
        Assert.Equal(0, scan.ExitCode);
        Assert.Contains("csharp.md", scan.Output);
        Assert.Contains("Found 3 questions", scan.Output);
        Assert.Contains("Questions discovered: 5", scan.Output);
        Assert.Contains("broken.md:1", scan.Output);
        Assert.Contains("Missing rc-prompt", scan.Output);

        CliResult create = await _cli.RunAsync("assessment", "create", "--count", "3");
        Assert.Equal(0, create.ExitCode);
        Assert.Contains("Assessment created.", create.Output);
        Assert.Contains("Questions: 3", create.Output);

        // Assert: the generated artifact is a valid assessment document.
        string assessmentFile = Assert.Single(
            Directory.GetFiles(_workspace.AssessmentsDirectory, "assessment-*-001.md"));
        string document = await File.ReadAllTextAsync(assessmentFile);

        Assert.StartsWith("---\n", document);
        Assert.Contains("type: assessment", document);
        Assert.Contains("question_count: 3", document);
        Assert.Contains("## Question 1", document);
        Assert.Contains("## Question 3", document);
        Assert.DoesNotContain(":::", document);
    }

    [Fact]
    public async Task Default_count_is_used_when_count_is_omitted()
    {
        _workspace.WriteQuestionFile("csharp.md", SampleQuestions.CSharpFile());
        await _cli.RunAsync("init");
        await _cli.RunAsync("source", "add", _workspace.QuestionsDirectory);

        CliResult create = await _cli.RunAsync("assessment", "create");

        // Only 3 questions exist, so all of them are included.
        Assert.Equal(0, create.ExitCode);
        Assert.Contains("Questions: 3", create.Output);
    }

    [Fact]
    public async Task Commands_requiring_a_workspace_fail_cleanly_before_init()
    {
        CliResult scan = await _cli.RunAsync("scan");

        Assert.Equal(1, scan.ExitCode);
        Assert.Contains("Run 'rc init' first", scan.Output);
    }

    [Fact]
    public async Task Init_is_idempotent()
    {
        CliResult first = await _cli.RunAsync("init");
        CliResult second = await _cli.RunAsync("init");

        Assert.Equal(0, first.ExitCode);
        Assert.Equal(0, second.ExitCode);
        Assert.Contains("already initialized", second.Output);
    }

    [Fact]
    public async Task Adding_a_missing_directory_fails()
    {
        await _cli.RunAsync("init");

        CliResult add = await _cli.RunAsync("source", "add", Path.Combine(_workspace.Root, "nope"));

        Assert.Equal(1, add.ExitCode);
        Assert.Contains("Directory not found", add.Output);
    }

    [Fact]
    public async Task Duplicate_source_registration_is_reported()
    {
        await _cli.RunAsync("init");
        await _cli.RunAsync("source", "add", _workspace.QuestionsDirectory);

        CliResult duplicate = await _cli.RunAsync("source", "add", _workspace.QuestionsDirectory);

        Assert.Equal(0, duplicate.ExitCode);
        Assert.Contains("already registered", duplicate.Output);
    }

    [Fact]
    public async Task Assessment_creation_without_questions_fails_cleanly()
    {
        await _cli.RunAsync("init");
        await _cli.RunAsync("source", "add", _workspace.QuestionsDirectory);

        CliResult create = await _cli.RunAsync("assessment", "create");

        Assert.Equal(1, create.ExitCode);
        Assert.Contains("No questions found", create.Output);
        Assert.False(Directory.Exists(_workspace.AssessmentsDirectory));
    }

    [Fact]
    public async Task Invalid_count_is_rejected_with_a_readable_error()
    {
        await _cli.RunAsync("init");

        CliResult create = await _cli.RunAsync("assessment", "create", "--count", "0");

        Assert.Equal(1, create.ExitCode);
        Assert.Contains("--count must be greater than zero", create.Output);
    }

    [Fact]
    public async Task Repeated_creation_produces_sequenced_files()
    {
        _workspace.WriteQuestionFile("csharp.md", SampleQuestions.CSharpFile());
        await _cli.RunAsync("init");
        await _cli.RunAsync("source", "add", _workspace.QuestionsDirectory);

        await _cli.RunAsync("assessment", "create", "--count", "2");
        await _cli.RunAsync("assessment", "create", "--count", "2");

        List<string?> files = Directory.GetFiles(_workspace.AssessmentsDirectory)
            .Select(Path.GetFileName)
            .Order()
            .ToList();

        Assert.Equal(2, files.Count);
        Assert.EndsWith("-001.md", files[0]);
        Assert.EndsWith("-002.md", files[1]);
    }
}
