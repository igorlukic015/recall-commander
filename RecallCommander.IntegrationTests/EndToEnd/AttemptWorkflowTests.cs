using Xunit;
using RecallCommander.IntegrationTests.Support;

namespace RecallCommander.IntegrationTests.EndToEnd;

/// <summary>
/// The attempt workflow through the CLI entry point: rc assessment create,
/// then the user's part — Save As plus writing answers under the '### Answer'
/// headings — simulated with plain file operations, then rc attempt validate.
/// The generated assessment file is never modified.
/// </summary>
public sealed class AttemptWorkflowTests : IDisposable
{
    private readonly TestWorkspace _workspace = new();
    private readonly CliRunner _cli;

    public AttemptWorkflowTests()
    {
        _cli = new CliRunner(_workspace);
    }

    public void Dispose() => _workspace.Dispose();

    private async Task<string> CreateAssessmentAsync()
    {
        _workspace.WriteQuestionFile("csharp.md", SampleQuestions.CSharpFile());
        await _cli.RunAsync("init");
        await _cli.RunAsync("source", "add", _workspace.QuestionsDirectory);

        var create = await _cli.RunAsync("assessment", "create", "--count", "3");
        Assert.Equal(0, create.ExitCode);

        return Assert.Single(Directory.GetFiles(_workspace.AssessmentsDirectory));
    }

    /// <summary>The user's Save As: copy the assessment into Attempts/ untouched.</summary>
    private string SaveAsAttempt(string assessmentPath)
    {
        var attemptsDirectory = Path.Combine(_workspace.Root, "Attempts");
        Directory.CreateDirectory(attemptsDirectory);

        var attemptPath = Path.Combine(
            attemptsDirectory,
            Path.GetFileNameWithoutExtension(assessmentPath) + ".attempt.md");
        File.Copy(assessmentPath, attemptPath);
        return attemptPath;
    }

    [Fact]
    public async Task Complete_workflow_from_assessment_to_validated_attempt()
    {
        var assessmentPath = await CreateAssessmentAsync();
        var assessmentBefore = await File.ReadAllTextAsync(assessmentPath);

        var attemptPath = SaveAsAttempt(assessmentPath);
        var document = await File.ReadAllTextAsync(attemptPath);
        await File.WriteAllTextAsync(
            attemptPath,
            document.Replace("### Answer", "### Answer\n\nMy answer, written in my own words."));

        var validate = await _cli.RunAsync("attempt", "validate", attemptPath);

        Assert.Equal(0, validate.ExitCode);
        Assert.Contains("Attempt is valid.", validate.Output);
        Assert.Contains("Questions: 3", validate.Output);
        Assert.Contains("Answered: 3", validate.Output);

        // The original assessment was never modified.
        Assert.Equal(assessmentBefore, await File.ReadAllTextAsync(assessmentPath));
    }

    [Fact]
    public async Task An_unanswered_attempt_is_valid_but_reported_as_unanswered()
    {
        var attemptPath = SaveAsAttempt(await CreateAssessmentAsync());

        var validate = await _cli.RunAsync("attempt", "validate", attemptPath);

        Assert.Equal(0, validate.ExitCode);
        Assert.Contains("Questions: 3", validate.Output);
        Assert.Contains("Answered: 0", validate.Output);
    }

    [Fact]
    public async Task A_deleted_answer_heading_is_reported_with_file_and_line()
    {
        var attemptPath = SaveAsAttempt(await CreateAssessmentAsync());

        // The user accidentally deletes the first Answer heading.
        var document = await File.ReadAllTextAsync(attemptPath);
        var index = document.IndexOf("### Answer", StringComparison.Ordinal);
        await File.WriteAllTextAsync(attemptPath, document.Remove(index, "### Answer\n".Length));

        var validate = await _cli.RunAsync("attempt", "validate", attemptPath);

        Assert.Equal(1, validate.ExitCode);
        Assert.Contains("Attempt is not valid.", validate.Output);
        Assert.Contains(".attempt.md:", validate.Output);
        Assert.Contains("Missing '### Answer' heading.", validate.Output);
    }

    [Fact]
    public async Task Validating_a_missing_file_fails_cleanly()
    {
        var validate = await _cli.RunAsync(
            "attempt", "validate", Path.Combine(_workspace.Root, "nope.attempt.md"));

        Assert.Equal(1, validate.ExitCode);
        Assert.Contains("File not found", validate.Output);
    }
}
