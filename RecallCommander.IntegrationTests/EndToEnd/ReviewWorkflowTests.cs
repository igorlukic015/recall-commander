using RecallCommander.IntegrationTests.Support;
using Xunit;

namespace RecallCommander.IntegrationTests.EndToEnd;

/// <summary>
/// The full review workflow through the CLI entry point: rc assessment
/// create, the user's Save As plus answers, rc attempt validate, then
/// rc review create. The review Markdown artifact must be self-contained:
/// original questions, the user's answers and the evaluation.
/// </summary>
public sealed class ReviewWorkflowTests : IDisposable
{
    private const string Answer = "My answer, written in my own words.";

    private readonly TestWorkspace _workspace = new();
    private readonly CliRunner _cli;

    public ReviewWorkflowTests()
    {
        _cli = new CliRunner(_workspace);
    }

    public void Dispose() => _workspace.Dispose();

    private async Task<string> CreateAssessmentAsync()
    {
        _workspace.WriteQuestionFile("csharp.md", SampleQuestions.CSharpFile());
        await _cli.RunAsync("init");
        await _cli.RunAsync("source", "add", _workspace.QuestionsDirectory);

        CliResult create = await _cli.RunAsync("assessment", "create", "--count", "3");
        Assert.Equal(0, create.ExitCode);

        return Assert.Single(Directory.GetFiles(_workspace.AssessmentsDirectory));
    }

    /// <summary>The user's Save As: copy the assessment into Attempts/ untouched.</summary>
    private string SaveAsAttempt(string assessmentPath)
    {
        string attemptsDirectory = Path.Combine(_workspace.Root, "Attempts");
        Directory.CreateDirectory(attemptsDirectory);

        string attemptPath = Path.Combine(
            attemptsDirectory,
            Path.GetFileNameWithoutExtension(assessmentPath) + ".attempt.md");
        File.Copy(assessmentPath, attemptPath);
        return attemptPath;
    }

    private async Task<string> CreateAnsweredAttemptAsync()
    {
        string attemptPath = SaveAsAttempt(await CreateAssessmentAsync());

        string document = await File.ReadAllTextAsync(attemptPath);
        await File.WriteAllTextAsync(
            attemptPath,
            document.Replace("### Answer", $"### Answer\n\n{Answer}"));

        return attemptPath;
    }

    [Fact]
    public async Task Complete_workflow_from_assessment_to_review_artifact()
    {
        string attemptPath = await CreateAnsweredAttemptAsync();
        string attemptBefore = await File.ReadAllTextAsync(attemptPath);

        CliResult validate = await _cli.RunAsync("attempt", "validate", attemptPath);
        Assert.Equal(0, validate.ExitCode);

        CliResult review = await _cli.RunAsync("review", "create", attemptPath);

        Assert.Equal(0, review.ExitCode);
        Assert.Contains("Review created.", review.Output);
        Assert.Contains("Questions: 3", review.Output);

        string reviewPath = Assert.Single(Directory.GetFiles(_workspace.ReviewsDirectory));
        string reviewId = Path.GetFileNameWithoutExtension(reviewPath);
        string document = await File.ReadAllTextAsync(reviewPath);

        // The id in frontmatter equals the file name.
        Assert.StartsWith("review-", reviewId);
        Assert.Contains("type: review", document);
        Assert.Contains($"id: {reviewId}", document);

        // The review references the attempt's assessment identity.
        string assessmentId = Path.GetFileNameWithoutExtension(
            Assert.Single(Directory.GetFiles(_workspace.AssessmentsDirectory)));
        Assert.Contains($"attempt: {assessmentId}", document);

        // Self-contained: original questions, the user's answers, evaluations.
        Assert.Contains("What is boxing in C#?", document);
        Assert.Contains(Answer, document);
        Assert.Contains("## Overall Summary", document);
        Assert.Contains("## Evaluation", document);
        Assert.Contains("Score: 8/10", document);
        Assert.Contains("Understanding: Strong", document);
        Assert.Contains("### Strengths", document);
        Assert.Contains("### Missing Information", document);
        Assert.Contains("### Incorrect Statements", document);
        Assert.Contains("### Suggestions", document);

        // The attempt file was never modified.
        Assert.Equal(attemptBefore, await File.ReadAllTextAsync(attemptPath));
    }

    [Fact]
    public async Task Each_review_gets_a_new_sequenced_artifact()
    {
        string attemptPath = await CreateAnsweredAttemptAsync();

        Assert.Equal(0, (await _cli.RunAsync("review", "create", attemptPath)).ExitCode);
        Assert.Equal(0, (await _cli.RunAsync("review", "create", attemptPath)).ExitCode);

        string[] reviews = Directory.GetFiles(_workspace.ReviewsDirectory);
        Assert.Equal(2, reviews.Length);
    }

    [Fact]
    public async Task An_invalid_attempt_is_rejected_with_diagnostics()
    {
        string attemptPath = SaveAsAttempt(await CreateAssessmentAsync());

        // The user accidentally deletes the first Answer heading.
        string document = await File.ReadAllTextAsync(attemptPath);
        int index = document.IndexOf("### Answer", StringComparison.Ordinal);
        await File.WriteAllTextAsync(attemptPath, document.Remove(index, "### Answer\n".Length));

        CliResult review = await _cli.RunAsync("review", "create", attemptPath);

        Assert.Equal(1, review.ExitCode);
        Assert.Contains("Attempt is not valid.", review.Output);
        Assert.Contains("Missing '### Answer' heading.", review.Output);
        Assert.False(Directory.Exists(_workspace.ReviewsDirectory));
    }

    [Fact]
    public async Task Reviewing_a_missing_file_fails_cleanly()
    {
        CliResult review = await _cli.RunAsync(
            "review", "create", Path.Combine(_workspace.Root, "nope.attempt.md"));

        Assert.Equal(1, review.ExitCode);
        Assert.Contains("File not found", review.Output);
    }
}
