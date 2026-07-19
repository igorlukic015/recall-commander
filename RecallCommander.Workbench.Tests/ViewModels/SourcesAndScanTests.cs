using RecallCommander.Workbench.Tests.Support;
using RecallCommander.Workbench.ViewModels;
using Xunit;

namespace RecallCommander.Workbench.Tests.ViewModels;

public sealed class SourcesAndScanTests
{
    private readonly WorkbenchHarness _harness = new();

    [Fact]
    public async Task Initialize_reports_the_workspace_and_loads_sources_and_assessments()
    {
        await _harness.AddRegisteredSourceAsync("/notes", "question");
        _harness.FileSystem
            .AddDirectory(WorkbenchHarness.AssessmentsDirectory)
            .AddFile($"{WorkbenchHarness.AssessmentsDirectory}/assessment-2026-07-18-001.md", "# Old");

        MainWindowViewModel viewModel = _harness.CreateViewModel();
        await viewModel.InitializeAsync();

        Assert.Contains("workspace", viewModel.OutputText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("/notes", Assert.Single(viewModel.Sources).DirectoryPath);
        Assert.Equal("assessment-2026-07-18-001.md", Assert.Single(viewModel.Assessments).FileName);
    }

    [Fact]
    public async Task Add_source_registers_the_picked_folder_and_reloads_the_list()
    {
        _harness.FileSystem.AddDirectory("/notes");
        _harness.Dialogs.FolderPicks.Enqueue("/notes");

        MainWindowViewModel viewModel = _harness.CreateViewModel();
        await viewModel.AddSourceCommand.ExecuteAsync(null);

        Assert.Contains("Registered question source: /notes", viewModel.OutputText);
        Assert.Equal("/notes", Assert.Single(viewModel.Sources).DirectoryPath);
    }

    [Fact]
    public async Task A_cancelled_folder_picker_changes_nothing()
    {
        MainWindowViewModel viewModel = _harness.CreateViewModel();
        await viewModel.AddSourceCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, viewModel.OutputText);
        Assert.Empty(viewModel.Sources);
    }

    [Fact]
    public async Task Refresh_sources_reloads_the_list()
    {
        MainWindowViewModel viewModel = _harness.CreateViewModel();
        await _harness.AddRegisteredSourceAsync("/notes", "question");

        await viewModel.RefreshSourcesCommand.ExecuteAsync(null);

        Assert.Single(viewModel.Sources);
        Assert.Contains("Sources refreshed: 1 registered.", viewModel.OutputText);
    }

    [Fact]
    public async Task Scan_reports_the_documented_summary_and_updates_the_knowledge_status()
    {
        await _harness.AddRegisteredSourceAsync("/notes", "question", "question", "warning:Question Block has no type.");

        MainWindowViewModel viewModel = _harness.CreateViewModel();
        await viewModel.ScanSourcesCommand.ExecuteAsync(null);

        Assert.Contains(
            """
            Scan completed.

            Sources: 1
            Files: 1
            Questions: 2
            Warnings: 1
            """.ReplaceLineEndings("\n"),
            viewModel.OutputText.ReplaceLineEndings("\n"));

        // Warnings stay visible in the output log.
        Assert.Contains("Warning: notes.md:3 — Question Block has no type.", viewModel.OutputText);

        Assert.Equal("Sources: 1 | Files: 1 | Questions: 2 | Warnings: 1", viewModel.KnowledgeStatusText);
    }

    [Fact]
    public async Task Scan_without_sources_prompts_to_add_one()
    {
        MainWindowViewModel viewModel = _harness.CreateViewModel();
        await viewModel.ScanSourcesCommand.ExecuteAsync(null);

        Assert.Contains("No question sources registered. Add a source first.", viewModel.OutputText);
        Assert.Contains("not scanned yet", viewModel.KnowledgeStatusText);
    }

    [Fact]
    public async Task An_uninitialized_workspace_becomes_a_readable_message()
    {
        MainWindowViewModel viewModel = _harness.CreateViewModel(new NotInitializedQuestionSourceRepository());
        await viewModel.ScanSourcesCommand.ExecuteAsync(null);

        Assert.Contains("Could not scan.", viewModel.OutputText);
        Assert.Contains("Initialize Recall Commander first.", viewModel.OutputText);
        Assert.DoesNotContain("WorkspaceNotInitializedException", viewModel.OutputText);
    }
}
