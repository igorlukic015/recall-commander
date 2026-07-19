using RecallCommander.Workbench.Services;
using RecallCommander.Workbench.Tests.Support;
using RecallCommander.Workbench.ViewModels;
using Xunit;

namespace RecallCommander.Workbench.Tests.ViewModels;

public sealed class AssessmentTests
{
    private readonly WorkbenchHarness _harness = new();

    [Fact]
    public async Task Create_assessment_reports_path_and_count_and_refreshes_the_list()
    {
        await _harness.AddRegisteredSourceAsync("/notes", "question", "question", "question");

        MainWindowViewModel viewModel = _harness.CreateViewModel();
        viewModel.QuestionCount = 3;
        await viewModel.CreateAssessmentCommand.ExecuteAsync(null);

        Assert.Contains("Assessment created.", viewModel.OutputText);
        Assert.Contains("Path:", viewModel.OutputText);
        Assert.Contains("assessment-2026-07-19-001.md", viewModel.OutputText);
        Assert.Contains("Questions:\n3", viewModel.OutputText.ReplaceLineEndings("\n"));

        // The Markdown file on disk is the source of truth for the list.
        Assert.Equal("assessment-2026-07-19-001.md", Assert.Single(viewModel.Assessments).FileName);
    }

    [Fact]
    public async Task Create_assessment_without_questions_explains_the_reason()
    {
        await _harness.AddRegisteredSourceAsync("/notes", "just prose, no questions");

        MainWindowViewModel viewModel = _harness.CreateViewModel();
        await viewModel.CreateAssessmentCommand.ExecuteAsync(null);

        Assert.Contains("Could not create assessment.", viewModel.OutputText);
        Assert.Contains("Reason:", viewModel.OutputText);
        Assert.Contains("No questions found.", viewModel.OutputText);
        Assert.Empty(viewModel.Assessments);
    }

    [Fact]
    public async Task An_invalid_question_count_is_rejected()
    {
        MainWindowViewModel viewModel = _harness.CreateViewModel();
        viewModel.QuestionCount = null;

        await viewModel.CreateAssessmentCommand.ExecuteAsync(null);

        Assert.Contains("Question count must be at least 1.", viewModel.OutputText);
    }

    [Fact]
    public void Refresh_assessments_lists_files_newest_first()
    {
        _harness.FileSystem
            .AddDirectory(WorkbenchHarness.AssessmentsDirectory)
            .AddFile($"{WorkbenchHarness.AssessmentsDirectory}/assessment-2026-07-18-001.md", "# Old")
            .AddFile($"{WorkbenchHarness.AssessmentsDirectory}/assessment-2026-07-19-002.md", "# Newest")
            .AddFile($"{WorkbenchHarness.AssessmentsDirectory}/assessment-2026-07-19-001.md", "# Newer");

        MainWindowViewModel viewModel = _harness.CreateViewModel();
        viewModel.RefreshAssessmentsCommand.Execute(null);

        Assert.Equal(
            [
                "assessment-2026-07-19-002.md",
                "assessment-2026-07-19-001.md",
                "assessment-2026-07-18-001.md",
            ],
            viewModel.Assessments.Select(file => file.FileName));
        Assert.Contains("Assessments refreshed: 3 found.", viewModel.OutputText);
    }

    [Fact]
    public void Preview_shows_the_selected_assessment_read_only()
    {
        string path = $"{WorkbenchHarness.AssessmentsDirectory}/assessment-2026-07-19-001.md";
        _harness.FileSystem
            .AddDirectory(WorkbenchHarness.AssessmentsDirectory)
            .AddFile(path, "# C# Assessment\n\n## Question 1\n");

        MainWindowViewModel viewModel = _harness.CreateViewModel();
        viewModel.RefreshAssessmentsCommand.Execute(null);
        viewModel.SelectedAssessment = viewModel.Assessments[0];

        viewModel.PreviewAssessmentCommand.Execute(null);

        Assert.Equal("# C# Assessment\n\n## Question 1\n", viewModel.PreviewText);
        Assert.Contains("assessment-2026-07-19-001.md", viewModel.PreviewTitle);
        Assert.Contains("read-only", viewModel.PreviewTitle);
    }

    [Fact]
    public void Preview_without_a_selection_asks_for_one()
    {
        MainWindowViewModel viewModel = _harness.CreateViewModel();
        viewModel.PreviewAssessmentCommand.Execute(null);

        Assert.Contains("Select an assessment to preview.", viewModel.OutputText);
    }

    [Fact]
    public void Open_externally_hands_the_file_to_the_operating_system()
    {
        string path = $"{WorkbenchHarness.AssessmentsDirectory}/assessment-2026-07-19-001.md";
        _harness.FileSystem
            .AddDirectory(WorkbenchHarness.AssessmentsDirectory)
            .AddFile(path, "# Assessment");

        MainWindowViewModel viewModel = _harness.CreateViewModel();
        viewModel.RefreshAssessmentsCommand.Execute(null);
        viewModel.SelectedAssessment = viewModel.Assessments[0];

        viewModel.OpenAssessmentExternallyCommand.Execute(null);

        Assert.Equal(path, Assert.Single(_harness.Opener.OpenedPaths));
        Assert.Contains("Use Save As in your editor", viewModel.OutputText);
    }
}
