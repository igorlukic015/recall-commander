using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecallCommander.Application.Assessments;
using RecallCommander.Application.Attempts;
using RecallCommander.Application.Reviews;
using RecallCommander.Application.Scanning;
using RecallCommander.Application.Sources;
using RecallCommander.Contracts.Exceptions;
using RecallCommander.Contracts.FileSystem;
using RecallCommander.Contracts.Parsing;
using RecallCommander.Contracts.Workspace;
using RecallCommander.Domain;
using RecallCommander.Workbench.Services;

namespace RecallCommander.Workbench.ViewModels;

/// <summary>
/// Drives the main window. All operations delegate to the same application
/// services the CLI commands use; this class only shapes their results into
/// bindable state and output messages. Markdown artifacts are displayed
/// read-only and opened externally for editing — the Workbench never writes
/// to them.
/// </summary>
public partial class MainWindowViewModel(
    IWorkspaceInitializer workspaceInitializer,
    QuestionSourceService sources,
    ScanService scanner,
    CreateAssessmentService assessments,
    ValidateAttemptService attempts,
    CreateReviewService reviews,
    AssessmentLocator assessmentLocator,
    IFileSystem fileSystem,
    IExternalFileOpener externalOpener,
    IDialogService dialogs) : ViewModelBase
{
    private const string PreviewPlaceholder =
        "Select an assessment (or an attempt via Select Attempt) and press Preview.\n\n"
        + "Artifacts are shown read-only. To edit, open them in your own editor.";

    private readonly StringBuilder _output = new();

    public ObservableCollection<QuestionSource> Sources { get; } = [];

    public ObservableCollection<ArtifactFile> Assessments { get; } = [];

    [ObservableProperty]
    public partial QuestionSource? SelectedSource { get; set; }

    [ObservableProperty]
    public partial ArtifactFile? SelectedAssessment { get; set; }

    [ObservableProperty]
    public partial string KnowledgeStatusText { get; private set; } =
        "Sources: – | Files: – | Questions: – | Warnings: – (not scanned yet)";

    [ObservableProperty]
    public partial decimal? QuestionCount { get; set; } = CreateAssessmentService.DefaultQuestionCount;

    [ObservableProperty]
    public partial string PreviewTitle { get; private set; } = "Preview";

    [ObservableProperty]
    public partial string PreviewText { get; private set; } = PreviewPlaceholder;

    [ObservableProperty]
    public partial string? SelectedAttemptPath { get; private set; }

    [ObservableProperty]
    public partial string SelectedAttemptText { get; private set; } = "No attempt selected";

    [ObservableProperty]
    public partial string OutputText { get; private set; } = string.Empty;

    /// <summary>
    /// Prepares the workspace (same as "rc init"; idempotent) and loads the
    /// registered sources and existing assessments. Called once when the main
    /// window opens.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            InitializationResult result = await workspaceInitializer.InitializeAsync();
            AppendOutput(result.Created
                ? "Initialized Recall Commander workspace."
                : "Workspace is already initialized.");
            AppendOutput($"Database: {result.DatabasePath}");

            await ReloadSourcesAsync();
            ReloadAssessments();
        }
        catch (Exception exception)
        {
            ReportFailure("Could not start the Workbench.", exception);
        }
    }

    [RelayCommand]
    private async Task AddSourceAsync()
    {
        try
        {
            string? path = await dialogs.PickFolderAsync("Select a question source directory");
            if (path is null)
            {
                return;
            }

            AddSourceResult result = await sources.AddAsync(path);

            AppendOutput(result.Status switch
            {
                AddSourceStatus.Added => $"Registered question source: {result.DirectoryPath}",
                AddSourceStatus.AlreadyRegistered => $"Question source is already registered: {result.DirectoryPath}",
                _ => $"Directory not found: {result.DirectoryPath}",
            });

            if (result.Status == AddSourceStatus.Added)
            {
                await ReloadSourcesAsync();
            }
        }
        catch (Exception exception)
        {
            ReportFailure("Could not add the source.", exception);
        }
    }

    [RelayCommand]
    private async Task RemoveSourceAsync()
    {
        try
        {
            if (SelectedSource is not { } source)
            {
                AppendOutput("Select a source to remove.");
                return;
            }

            RemoveSourceResult result = await sources.RemoveAsync(source.DirectoryPath);

            AppendOutput(result.Status == RemoveSourceStatus.Removed
                ? $"Removed question source: {result.DirectoryPath}"
                : $"Question source is not registered: {result.DirectoryPath}");

            await ReloadSourcesAsync();
        }
        catch (Exception exception)
        {
            ReportFailure("Could not remove the source.", exception);
        }
    }

    [RelayCommand]
    private async Task RefreshSourcesAsync()
    {
        try
        {
            await ReloadSourcesAsync();
            AppendOutput($"Sources refreshed: {Sources.Count} registered.");
        }
        catch (Exception exception)
        {
            ReportFailure("Could not refresh the sources.", exception);
        }
    }

    [RelayCommand]
    private async Task ScanSourcesAsync()
    {
        try
        {
            AppendOutput("Scanning...");

            ScanReport report = await scanner.ScanAsync();

            if (report.SourceCount == 0)
            {
                AppendOutput("No question sources registered. Add a source first.");
                return;
            }

            foreach (ScanWarning warning in report.Warnings)
            {
                AppendOutput($"Warning: {warning.Location} — {warning.Message}");
            }

            KnowledgeStatusText =
                $"Sources: {report.SourceCount} | Files: {report.Files.Count} | "
                + $"Questions: {report.TotalQuestions} | Warnings: {report.Warnings.Count}";

            AppendOutput(
                $"""
                 Scan completed.

                 Sources: {report.SourceCount}
                 Files: {report.Files.Count}
                 Questions: {report.TotalQuestions}
                 Warnings: {report.Warnings.Count}
                 """);
        }
        catch (Exception exception)
        {
            ReportFailure("Could not scan.", exception);
        }
    }

    [RelayCommand]
    private async Task CreateAssessmentAsync()
    {
        try
        {
            if (QuestionCount is not { } count || count < 1)
            {
                AppendOutput("Question count must be at least 1.");
                return;
            }

            CreateAssessmentResult result = await assessments.CreateAsync((int)count);

            if (result.Status == CreateAssessmentStatus.NoQuestionsFound)
            {
                AppendOutput(
                    """
                    Could not create assessment.

                    Reason:
                    No questions found. Scan your sources to check.
                    """);
                return;
            }

            string displayPath = Path.GetRelativePath(Environment.CurrentDirectory, result.FilePath!);
            AppendOutput(
                $"""
                 Assessment created.

                 Path:
                 {displayPath}

                 Questions:
                 {result.QuestionCount}
                 """);

            ReloadAssessments();
        }
        catch (Exception exception)
        {
            ReportFailure("Could not create assessment.", exception);
        }
    }

    [RelayCommand]
    private void RefreshAssessments()
    {
        try
        {
            ReloadAssessments();
            AppendOutput($"Assessments refreshed: {Assessments.Count} found.");
        }
        catch (Exception exception)
        {
            ReportFailure("Could not refresh the assessments.", exception);
        }
    }

    [RelayCommand]
    private void PreviewAssessment()
    {
        if (SelectedAssessment is not { } assessment)
        {
            AppendOutput("Select an assessment to preview.");
            return;
        }

        Preview(assessment.FileName, assessment.FilePath);
    }

    [RelayCommand]
    private void OpenAssessmentExternally()
    {
        if (SelectedAssessment is not { } assessment)
        {
            AppendOutput("Select an assessment to open.");
            return;
        }

        try
        {
            externalOpener.Open(assessment.FilePath);
            AppendOutput($"Opened externally: {assessment.FileName}");
            AppendOutput("Use Save As in your editor to turn it into an attempt.");
        }
        catch (Exception exception)
        {
            ReportFailure("Could not open the assessment externally.", exception);
        }
    }

    [RelayCommand]
    private async Task SelectAttemptAsync()
    {
        try
        {
            string? path = await dialogs.PickFileAsync(
                "Select a completed assessment file",
                "Markdown files",
                ["*.md"]);

            if (path is null)
            {
                return;
            }

            SelectedAttemptPath = path;
            SelectedAttemptText = $"Attempt: {Path.GetFileName(path)}";
            Preview(Path.GetFileName(path), path);
        }
        catch (Exception exception)
        {
            ReportFailure("Could not select the attempt.", exception);
        }
    }

    [RelayCommand]
    private async Task ValidateAttemptAsync()
    {
        try
        {
            if (SelectedAttemptPath is null)
            {
                await SelectAttemptAsync();
            }

            if (SelectedAttemptPath is not { } path)
            {
                return;
            }

            ValidateAttemptResult result = attempts.Validate(path);

            switch (result.Status)
            {
                case ValidateAttemptStatus.FileNotFound:
                    AppendOutput($"File not found: {result.FilePath}");
                    break;

                case ValidateAttemptStatus.Invalid:
                    AppendOutput("Attempt is not valid.");
                    foreach (ParseDiagnostic diagnostic in result.Diagnostics)
                    {
                        AppendOutput($"  {result.FilePath}:{diagnostic.LineNumber} — {diagnostic.Message}");
                    }

                    break;

                default:
                    Attempt attempt = result.Attempt!;
                    AppendOutput("Attempt is valid.");
                    AppendOutput($"  Title: {attempt.Title}");
                    AppendOutput($"  Questions: {attempt.Questions.Count}");
                    AppendOutput($"  Answered: {attempt.Questions.Count(question => question.IsAnswered)}");
                    break;
            }
        }
        catch (Exception exception)
        {
            ReportFailure("Could not validate the attempt.", exception);
        }
    }

    [RelayCommand]
    private async Task CreateReviewAsync()
    {
        try
        {
            if (SelectedAttemptPath is null)
            {
                await SelectAttemptAsync();
            }

            if (SelectedAttemptPath is not { } path)
            {
                return;
            }

            AppendOutput("Creating review...");

            CreateReviewResult result = await reviews.CreateAsync(path);

            switch (result.Status)
            {
                case CreateReviewStatus.FileNotFound:
                    AppendOutput($"File not found: {result.AttemptFilePath}");
                    break;

                case CreateReviewStatus.InvalidAttempt:
                    AppendOutput("Attempt is not valid. Fix it and validate again.");
                    foreach (ParseDiagnostic diagnostic in result.Diagnostics)
                    {
                        AppendOutput($"  {result.AttemptFilePath}:{diagnostic.LineNumber} — {diagnostic.Message}");
                    }

                    break;

                default:
                    AppendOutput(
                        $"""
                         Review created.

                         Path:
                         {result.ReviewFilePath}

                         Questions:
                         {result.QuestionCount}
                         """);
                    Preview(Path.GetFileName(result.ReviewFilePath!), result.ReviewFilePath!);
                    break;
            }
        }
        catch (Exception exception)
        {
            ReportFailure("Could not create the review.", exception);
        }
    }

    /// <summary>Loads an artifact into the read-only preview. Never writes anything back.</summary>
    private void Preview(string fileName, string filePath)
    {
        try
        {
            PreviewText = fileSystem.ReadAllText(filePath);
            PreviewTitle = $"Preview — {fileName} (read-only)";
        }
        catch (Exception exception)
        {
            ReportFailure("Could not preview the file.", exception);
        }
    }

    private async Task ReloadSourcesAsync()
    {
        IReadOnlyList<QuestionSource> all = await sources.ListAsync();

        Sources.Clear();
        foreach (QuestionSource source in all)
        {
            Sources.Add(source);
        }
    }

    private void ReloadAssessments()
    {
        ArtifactFile? previouslySelected = SelectedAssessment;

        Assessments.Clear();
        foreach (ArtifactFile assessment in assessmentLocator.List())
        {
            Assessments.Add(assessment);
        }

        SelectedAssessment = previouslySelected is null
            ? null
            : Assessments.FirstOrDefault(file => file.FilePath == previouslySelected.FilePath);
    }

    /// <summary>Turns an exception into a readable output block; no stack traces.</summary>
    private void ReportFailure(string headline, Exception exception)
    {
        string reason = exception is WorkspaceNotInitializedException
            ? "Initialize Recall Commander first."
            : exception.Message;

        AppendOutput(
            $"""
             {headline}

             Reason:
             {reason}
             """);
    }

    private void AppendOutput(string message)
    {
        _output.AppendLine(message);
        OutputText = _output.ToString();
    }
}
