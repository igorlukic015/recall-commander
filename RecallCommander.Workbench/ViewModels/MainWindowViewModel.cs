using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecallCommander.Application.Abstractions;
using RecallCommander.Application.Assessments;
using RecallCommander.Application.Attempts;
using RecallCommander.Application.Scanning;
using RecallCommander.Application.Sources;
using RecallCommander.Domain;
using RecallCommander.Workbench.Services;

namespace RecallCommander.Workbench.ViewModels;

/// <summary>
/// Drives the main window. All operations delegate to the same application
/// services the CLI commands use; this class only shapes their results into
/// bindable state and output messages.
/// </summary>
public partial class MainWindowViewModel(
    IWorkspaceInitializer workspaceInitializer,
    QuestionSourceService sources,
    ScanService scanner,
    CreateAssessmentService assessments,
    ValidateAttemptService attempts,
    IDialogService dialogs) : ViewModelBase
{
    private readonly StringBuilder _output = new();

    public ObservableCollection<QuestionSource> Sources { get; } = [];

    [ObservableProperty]
    public partial QuestionSource? SelectedSource { get; set; }

    [ObservableProperty]
    public partial string DiscoveredQuestionsText { get; private set; } = "Discovered questions: not scanned yet";

    [ObservableProperty]
    public partial decimal? QuestionCount { get; set; } = CreateAssessmentService.DefaultQuestionCount;

    [ObservableProperty]
    public partial string OutputText { get; private set; } = string.Empty;

    /// <summary>
    /// Prepares the workspace (same as "rc init"; idempotent) and loads the
    /// registered sources. Called once when the main window opens.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var result = await workspaceInitializer.InitializeAsync();
            AppendOutput(result.Created
                ? "Initialized Recall Commander workspace."
                : "Workspace is already initialized.");
            AppendOutput($"Database: {result.DatabasePath}");

            await ReloadSourcesAsync();
        }
        catch (Exception exception)
        {
            AppendOutput($"Startup failed: {exception.Message}");
        }
    }

    [RelayCommand]
    private async Task AddSourceAsync()
    {
        try
        {
            var path = await dialogs.PickFolderAsync("Select a question source directory");
            if (path is null)
            {
                return;
            }

            var result = await sources.AddAsync(path);

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
            AppendOutput($"Add source failed: {exception.Message}");
        }
    }

    [RelayCommand]
    private async Task ScanSourcesAsync()
    {
        try
        {
            AppendOutput("Scanning...");

            var report = await scanner.ScanAsync();

            if (report.SourceCount == 0)
            {
                AppendOutput("No question sources registered. Add a source first.");
                return;
            }

            foreach (var warning in report.Warnings)
            {
                AppendOutput($"Warning: {warning.Location} — {warning.Message}");
            }

            DiscoveredQuestionsText = $"Discovered questions: {report.TotalQuestions}";
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
            AppendOutput($"Scan failed: {exception.Message}");
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

            var result = await assessments.CreateAsync((int)count);

            if (result.Status == CreateAssessmentStatus.NoQuestionsFound)
            {
                AppendOutput("No questions found. Check your sources with a scan.");
                return;
            }

            var displayPath = Path.GetRelativePath(Environment.CurrentDirectory, result.FilePath!);
            AppendOutput(
                $"""
                 Assessment created.

                 Path:
                 {displayPath}

                 Questions:
                 {result.QuestionCount}
                 """);
        }
        catch (Exception exception)
        {
            AppendOutput($"Create assessment failed: {exception.Message}");
        }
    }

    [RelayCommand]
    private async Task ValidateAttemptAsync()
    {
        try
        {
            var path = await dialogs.PickFileAsync(
                "Select a completed assessment file",
                "Markdown files",
                ["*.md"]);

            if (path is null)
            {
                return;
            }

            var result = attempts.Validate(path);

            switch (result.Status)
            {
                case ValidateAttemptStatus.FileNotFound:
                    AppendOutput($"File not found: {result.FilePath}");
                    break;

                case ValidateAttemptStatus.Invalid:
                    AppendOutput("Attempt is not valid.");
                    foreach (var diagnostic in result.Diagnostics)
                    {
                        AppendOutput($"  {result.FilePath}:{diagnostic.LineNumber} — {diagnostic.Message}");
                    }

                    break;

                default:
                    var attempt = result.Attempt!;
                    AppendOutput("Attempt is valid.");
                    AppendOutput($"  Title: {attempt.Title}");
                    AppendOutput($"  Questions: {attempt.Questions.Count}");
                    AppendOutput($"  Answered: {attempt.Questions.Count(question => question.IsAnswered)}");
                    break;
            }
        }
        catch (Exception exception)
        {
            AppendOutput($"Validate attempt failed: {exception.Message}");
        }
    }

    private async Task ReloadSourcesAsync()
    {
        var all = await sources.ListAsync();

        Sources.Clear();
        foreach (var source in all)
        {
            Sources.Add(source);
        }
    }

    private void AppendOutput(string message)
    {
        _output.AppendLine(message);
        OutputText = _output.ToString();
    }
}
