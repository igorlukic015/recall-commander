using RecallCommander.Contracts.Artifacts;
using RecallCommander.Contracts.Attempts;
using RecallCommander.Contracts.FileSystem;
using RecallCommander.Contracts.Parsing;
using RecallCommander.Contracts.Questions;
using RecallCommander.Contracts.Sources;
using RecallCommander.Contracts.Workspace;
using RecallCommander.Domain;
using RecallCommander.Workbench.Services;

namespace RecallCommander.Workbench.Tests.Support;

/// <summary>Answers dialog requests from prepared queues; empty queue means the user cancelled.</summary>
public sealed class FakeDialogService : IDialogService
{
    public Queue<string?> FolderPicks { get; } = new();

    public Queue<string?> FilePicks { get; } = new();

    public int FilePickRequests { get; private set; }

    public Task<string?> PickFolderAsync(string title) =>
        Task.FromResult(FolderPicks.Count > 0 ? FolderPicks.Dequeue() : null);

    public Task<string?> PickFileAsync(string title, string filterName, IReadOnlyList<string> patterns)
    {
        FilePickRequests++;
        return Task.FromResult(FilePicks.Count > 0 ? FilePicks.Dequeue() : null);
    }
}

/// <summary>Records the files handed to the operating system instead of launching anything.</summary>
public sealed class FakeExternalFileOpener : IExternalFileOpener
{
    public List<string> OpenedPaths { get; } = [];

    public void Open(string filePath) => OpenedPaths.Add(filePath);
}

public sealed class FakeWorkspaceInitializer(bool created = true) : IWorkspaceInitializer
{
    public Task<InitializationResult> InitializeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new InitializationResult(created, "/data/recall.db"));
}

public sealed class FakeFileSystem : IFileSystem
{
    private readonly HashSet<string> _directories = [];
    private readonly Dictionary<string, string> _files = [];

    public FakeFileSystem AddDirectory(string path)
    {
        _directories.Add(path);
        return this;
    }

    public FakeFileSystem AddFile(string path, string content)
    {
        _files[path] = content;
        return this;
    }

    public string NormalizePath(string path) => path;

    public bool DirectoryExists(string path) => _directories.Contains(path);

    public bool FileExists(string path) => _files.ContainsKey(path);

    public IEnumerable<string> EnumerateMarkdownFiles(string directoryPath) =>
        _files.Keys
            .Where(file => file.StartsWith(directoryPath + "/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal);

    public string ReadAllText(string filePath) => _files[filePath];
}

public sealed class InMemoryQuestionSourceRepository : IQuestionSourceRepository
{
    private readonly List<QuestionSource> _sources = [];

    public Task<QuestionSource> AddAsync(
        string directoryPath,
        DateTimeOffset registeredAtUtc,
        CancellationToken cancellationToken = default)
    {
        QuestionSource source = new QuestionSource(_sources.Count + 1, directoryPath, registeredAtUtc);
        _sources.Add(source);
        return Task.FromResult(source);
    }

    public Task<IReadOnlyList<QuestionSource>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<QuestionSource>>(_sources.ToList());

    public Task<bool> ExistsAsync(string directoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sources.Any(source => source.DirectoryPath == directoryPath));
}

/// <summary>Every call fails as if 'rc init' never ran.</summary>
public sealed class NotInitializedQuestionSourceRepository : IQuestionSourceRepository
{
    public Task<QuestionSource> AddAsync(
        string directoryPath,
        DateTimeOffset registeredAtUtc,
        CancellationToken cancellationToken = default) =>
        throw new Contracts.Exceptions.WorkspaceNotInitializedException();

    public Task<IReadOnlyList<QuestionSource>> GetAllAsync(CancellationToken cancellationToken = default) =>
        throw new Contracts.Exceptions.WorkspaceNotInitializedException();

    public Task<bool> ExistsAsync(string directoryPath, CancellationToken cancellationToken = default) =>
        throw new Contracts.Exceptions.WorkspaceNotInitializedException();
}

/// <summary>
/// Interprets each input line as a directive: "question" produces one discovered
/// question, "warning:&lt;message&gt;" produces one diagnostic.
/// </summary>
public sealed class FakeQuestionBlockParser : IQuestionBlockParser
{
    public QuestionParseResult Parse(string markdown)
    {
        List<DiscoveredQuestion> questions = new List<DiscoveredQuestion>();
        List<ParseDiagnostic> diagnostics = new List<ParseDiagnostic>();

        string[] lines = markdown.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index].Trim();
            if (line == "question")
            {
                questions.Add(new DiscoveredQuestion(
                    new Question(QuestionType.Recall, "What?"),
                    index + 1));
            }
            else if (line.StartsWith("warning:", StringComparison.Ordinal))
            {
                diagnostics.Add(new ParseDiagnostic(index + 1, line["warning:".Length..]));
            }
        }

        return new QuestionParseResult(questions, diagnostics);
    }
}

public sealed class FakeAttemptParser(AttemptParseResult result) : IAttemptParser
{
    public string? LastMarkdown { get; private set; }

    public AttemptParseResult Parse(string markdown)
    {
        LastMarkdown = markdown;
        return result;
    }
}

/// <summary>
/// Pretends to write an assessment artifact: records it and drops a Markdown
/// file into the fake filesystem so the assessment list can discover it.
/// </summary>
public sealed class RecordingAssessmentWriter(FakeFileSystem fileSystem, string directory) : IArtifactWriter<Assessment>
{
    private int _sequence;

    public Assessment? Written { get; private set; }

    public Task<SavedArtifact> WriteAsync(Assessment artifact, CancellationToken cancellationToken = default)
    {
        Written = artifact;
        _sequence++;

        string path = $"{directory}/assessment-2026-07-19-{_sequence:000}.md";
        fileSystem.AddDirectory(directory).AddFile(path, $"# {artifact.Title}\n\ngenerated assessment body\n");

        return Task.FromResult(new SavedArtifact(path));
    }
}

/// <summary>
/// Pretends to write a review artifact: records it and drops a Markdown file
/// into the fake filesystem so the created review can be previewed.
/// </summary>
public sealed class RecordingReviewWriter(FakeFileSystem fileSystem, string directory) : IArtifactWriter<Review>
{
    private int _sequence;

    public Review? Written { get; private set; }

    public Task<SavedArtifact> WriteAsync(Review artifact, CancellationToken cancellationToken = default)
    {
        Written = artifact;
        _sequence++;

        string path = $"{directory}/review-2026-07-19-{_sequence:000}.md";
        fileSystem.AddDirectory(directory).AddFile(path, $"# {artifact.Title}\n\ngenerated review body\n");

        return Task.FromResult(new SavedArtifact(path));
    }
}

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

public sealed class FixedArtifactOutputPathProvider(string directory) : IArtifactOutputPathProvider
{
    public string GetOutputDirectory() => directory;
}
