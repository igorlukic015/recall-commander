using RecallCommander.Application.Assessments;
using RecallCommander.Application.Attempts;
using RecallCommander.Application.Reviews;
using RecallCommander.Application.Scanning;
using RecallCommander.Application.Sources;
using RecallCommander.Contracts.Assessments;
using RecallCommander.Contracts.Attempts;
using RecallCommander.Contracts.Sources;
using RecallCommander.Domain;
using RecallCommander.Markdown.Writing;
using RecallCommander.Workbench.Services;
using RecallCommander.Workbench.ViewModels;

namespace RecallCommander.Workbench.Tests.Support;

/// <summary>
/// Wires a <see cref="MainWindowViewModel"/> exactly like the production
/// composition root — real Application services — with only the boundaries
/// faked: filesystem, repository, dialogs, artifact writer, external opener.
/// </summary>
public sealed class WorkbenchHarness
{
    public const string OutputDirectory = "/output";
    public const string AssessmentsDirectory = "/output/Assessments";
    public const string ReviewsDirectory = "/output/Reviews";

    private static readonly DateTimeOffset Now = new(2026, 7, 19, 20, 0, 0, TimeSpan.Zero);

    public WorkbenchHarness()
    {
        Writer = new RecordingAssessmentWriter(FileSystem, AssessmentsDirectory);
        ReviewWriter = new RecordingReviewWriter(FileSystem, ReviewsDirectory);
    }

    public FakeFileSystem FileSystem { get; } = new();

    public InMemoryQuestionSourceRepository Repository { get; } = new();

    public FakeDialogService Dialogs { get; } = new();

    public FakeExternalFileOpener Opener { get; } = new();

    public RecordingAssessmentWriter Writer { get; }

    public RecordingReviewWriter ReviewWriter { get; }

    /// <summary>What the attempt parser returns; defaults to a valid attempt.</summary>
    public AttemptParseResult AttemptResult { get; set; } = new(
        new Attempt(
            "C# Assessment",
            Now,
            [
                new AttemptQuestion("What is boxing?", "An answer."),
                new AttemptQuestion("What is the CLR?", ""),
            ]),
        []);

    /// <summary>Registers a source directory holding one file with the given parser directives.</summary>
    public async Task AddRegisteredSourceAsync(string directory, params string[] fileLines)
    {
        await Repository.AddAsync(directory, Now);
        FileSystem
            .AddDirectory(directory)
            .AddFile($"{directory}/notes.md", string.Join('\n', fileLines));
    }

    public MainWindowViewModel CreateViewModel(IQuestionSourceRepository? repository = null)
    {
        IQuestionSourceRepository repo = repository ?? Repository;
        FixedTimeProvider time = new FixedTimeProvider(Now);
        ScanService scanner = new ScanService(repo, FileSystem, new FakeQuestionBlockParser());
        ValidateAttemptService attempts = new ValidateAttemptService(FileSystem, new FakeAttemptParser(AttemptResult));

        return new MainWindowViewModel(
            new FakeWorkspaceInitializer(),
            new QuestionSourceService(repo, FileSystem, time),
            scanner,
            new CreateAssessmentService(scanner, new FirstNSelector(), Writer, time),
            attempts,
            new CreateReviewService(attempts, new FakeQuestionEvaluator(), ReviewWriter, time),
            new AssessmentLocator(
                new FixedArtifactOutputPathProvider(OutputDirectory),
                new AssessmentRenderer(),
                FileSystem),
            FileSystem,
            Opener,
            Dialogs);
    }

    private sealed class FirstNSelector : IQuestionSelector
    {
        public IReadOnlyList<Question> Select(IReadOnlyList<Question> questions, int count) =>
            questions.Take(count).ToList();
    }
}
