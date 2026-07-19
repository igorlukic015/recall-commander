using RecallCommander.Contracts.Attempts;
using RecallCommander.Contracts.Parsing;
using RecallCommander.Workbench.Tests.Support;
using RecallCommander.Workbench.ViewModels;
using Xunit;

namespace RecallCommander.Workbench.Tests.ViewModels;

public sealed class AttemptTests
{
    private const string AttemptPath = "/attempts/assessment-2026-07-19-001.attempt.md";
    private const string AttemptContent = "# C# Assessment\n\n## Question 1\n\n### Answer\n\nMy answer.\n";

    private readonly WorkbenchHarness _harness = new();

    private void AddAttemptFile() => _harness.FileSystem.AddFile(AttemptPath, AttemptContent);

    [Fact]
    public async Task Select_attempt_remembers_the_file_and_previews_it_read_only()
    {
        AddAttemptFile();
        _harness.Dialogs.FilePicks.Enqueue(AttemptPath);

        MainWindowViewModel viewModel = _harness.CreateViewModel();
        await viewModel.SelectAttemptCommand.ExecuteAsync(null);

        Assert.Equal(AttemptPath, viewModel.SelectedAttemptPath);
        Assert.Equal("Attempt: assessment-2026-07-19-001.attempt.md", viewModel.SelectedAttemptText);
        Assert.Equal(AttemptContent, viewModel.PreviewText);
        Assert.Contains("read-only", viewModel.PreviewTitle);
    }

    [Fact]
    public async Task Validate_reports_a_valid_attempt()
    {
        AddAttemptFile();
        _harness.Dialogs.FilePicks.Enqueue(AttemptPath);

        MainWindowViewModel viewModel = _harness.CreateViewModel();
        await viewModel.ValidateAttemptCommand.ExecuteAsync(null);

        Assert.Contains("Attempt is valid.", viewModel.OutputText);
        Assert.Contains("Title: C# Assessment", viewModel.OutputText);
        Assert.Contains("Questions: 2", viewModel.OutputText);
        Assert.Contains("Answered: 1", viewModel.OutputText);
    }

    [Fact]
    public async Task Validate_reuses_the_selected_attempt_without_asking_again()
    {
        AddAttemptFile();
        _harness.Dialogs.FilePicks.Enqueue(AttemptPath);

        MainWindowViewModel viewModel = _harness.CreateViewModel();
        await viewModel.SelectAttemptCommand.ExecuteAsync(null);
        await viewModel.ValidateAttemptCommand.ExecuteAsync(null);

        Assert.Equal(1, _harness.Dialogs.FilePickRequests);
        Assert.Contains("Attempt is valid.", viewModel.OutputText);
    }

    [Fact]
    public async Task Validate_lists_diagnostics_for_an_invalid_attempt()
    {
        AddAttemptFile();
        _harness.Dialogs.FilePicks.Enqueue(AttemptPath);
        _harness.AttemptResult = new AttemptParseResult(
            Attempt: null,
            [
                new ParseDiagnostic(7, "Missing '### Answer' heading."),
                new ParseDiagnostic(13, "Question has no prompt text."),
            ]);

        MainWindowViewModel viewModel = _harness.CreateViewModel();
        await viewModel.ValidateAttemptCommand.ExecuteAsync(null);

        Assert.Contains("Attempt is not valid.", viewModel.OutputText);
        Assert.Contains($"{AttemptPath}:7 — Missing '### Answer' heading.", viewModel.OutputText);
        Assert.Contains($"{AttemptPath}:13 — Question has no prompt text.", viewModel.OutputText);
    }

    [Fact]
    public async Task Validate_reports_a_missing_file()
    {
        _harness.Dialogs.FilePicks.Enqueue("/attempts/nope.md");

        MainWindowViewModel viewModel = _harness.CreateViewModel();
        await viewModel.ValidateAttemptCommand.ExecuteAsync(null);

        Assert.Contains("File not found: /attempts/nope.md", viewModel.OutputText);
    }

    [Fact]
    public async Task A_cancelled_file_picker_changes_nothing()
    {
        MainWindowViewModel viewModel = _harness.CreateViewModel();
        await viewModel.ValidateAttemptCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, viewModel.OutputText);
        Assert.Null(viewModel.SelectedAttemptPath);
    }
}
