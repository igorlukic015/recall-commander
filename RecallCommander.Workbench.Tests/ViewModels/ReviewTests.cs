using RecallCommander.Contracts.Attempts;
using RecallCommander.Contracts.Parsing;
using RecallCommander.Workbench.Tests.Support;
using RecallCommander.Workbench.ViewModels;
using Xunit;

namespace RecallCommander.Workbench.Tests.ViewModels;

public sealed class ReviewTests
{
    private const string AttemptPath = "/attempts/assessment-2026-07-19-001.attempt.md";
    private const string AttemptContent = "# C# Assessment\n\n## Question 1\n\n### Answer\n\nMy answer.\n";

    private readonly WorkbenchHarness _harness = new();

    private void AddAttemptFile() => _harness.FileSystem.AddFile(AttemptPath, AttemptContent);

    [Fact]
    public async Task Create_review_evaluates_the_attempt_and_previews_the_artifact()
    {
        AddAttemptFile();
        _harness.Dialogs.FilePicks.Enqueue(AttemptPath);

        MainWindowViewModel viewModel = _harness.CreateViewModel();
        await viewModel.CreateReviewCommand.ExecuteAsync(null);

        Assert.Contains("Review created.", viewModel.OutputText);
        Assert.Contains($"{WorkbenchHarness.ReviewsDirectory}/review-2026-07-19-001.md", viewModel.OutputText);
        Assert.Contains("Questions:\n2", viewModel.OutputText);

        Assert.NotNull(_harness.ReviewWriter.Written);
        Assert.Equal("Review - C# Assessment", _harness.ReviewWriter.Written.Title);
        Assert.Equal(2, _harness.ReviewWriter.Written.QuestionReviews.Count);
        Assert.Equal("fake", _harness.ReviewWriter.Written.Evaluator);

        Assert.Contains("generated review body", viewModel.PreviewText);
        Assert.Contains("review-2026-07-19-001.md", viewModel.PreviewTitle);
        Assert.Contains("read-only", viewModel.PreviewTitle);
    }

    [Fact]
    public async Task Create_review_reuses_the_selected_attempt_without_asking_again()
    {
        AddAttemptFile();
        _harness.Dialogs.FilePicks.Enqueue(AttemptPath);

        MainWindowViewModel viewModel = _harness.CreateViewModel();
        await viewModel.SelectAttemptCommand.ExecuteAsync(null);
        await viewModel.CreateReviewCommand.ExecuteAsync(null);

        Assert.Equal(1, _harness.Dialogs.FilePickRequests);
        Assert.Contains("Review created.", viewModel.OutputText);
    }

    [Fact]
    public async Task Create_review_lists_diagnostics_for_an_invalid_attempt()
    {
        AddAttemptFile();
        _harness.Dialogs.FilePicks.Enqueue(AttemptPath);
        _harness.AttemptResult = new AttemptParseResult(
            Attempt: null,
            [new ParseDiagnostic(7, "Missing '### Answer' heading.")]);

        MainWindowViewModel viewModel = _harness.CreateViewModel();
        await viewModel.CreateReviewCommand.ExecuteAsync(null);

        Assert.Contains("Attempt is not valid.", viewModel.OutputText);
        Assert.Contains($"{AttemptPath}:7 — Missing '### Answer' heading.", viewModel.OutputText);
        Assert.Null(_harness.ReviewWriter.Written);
    }

    [Fact]
    public async Task Create_review_reports_a_missing_file()
    {
        _harness.Dialogs.FilePicks.Enqueue("/attempts/nope.md");

        MainWindowViewModel viewModel = _harness.CreateViewModel();
        await viewModel.CreateReviewCommand.ExecuteAsync(null);

        Assert.Contains("File not found: /attempts/nope.md", viewModel.OutputText);
        Assert.Null(_harness.ReviewWriter.Written);
    }

    [Fact]
    public async Task A_cancelled_file_picker_changes_nothing()
    {
        MainWindowViewModel viewModel = _harness.CreateViewModel();
        await viewModel.CreateReviewCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, viewModel.OutputText);
        Assert.Null(_harness.ReviewWriter.Written);
    }
}
