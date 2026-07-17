using RecallCommander.Application.Artifacts;
using RecallCommander.Application.Assessments;
using RecallCommander.Application.Scanning;
using RecallCommander.Application.Tests.Fakes;
using RecallCommander.Contracts.Artifacts;
using RecallCommander.Contracts.Assessments;
using RecallCommander.Domain;
using Xunit;

namespace RecallCommander.Application.Tests.Assessments;

public sealed class CreateAssessmentServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 16, 18, 0, 0, TimeSpan.Zero);

    private readonly InMemoryQuestionSourceRepository _repository = new();
    private readonly FakeFileSystem _fileSystem = new();
    private readonly RecordingWriter _writer = new();

    private sealed class FirstNSelector : IQuestionSelector
    {
        public IReadOnlyList<Question> Select(IReadOnlyList<Question> questions, int count) =>
            questions.Take(count).ToList();
    }

    private sealed class RecordingWriter : IArtifactWriter<Assessment>
    {
        public Assessment? Written { get; private set; }

        public Task<SavedArtifact> WriteAsync(Assessment artifact, CancellationToken cancellationToken = default)
        {
            Written = artifact;
            return Task.FromResult(new SavedArtifact("/output/Assessments/assessment-2026-07-16-001.md"));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private CreateAssessmentService CreateService() => new(
        new ScanService(_repository, _fileSystem, new FakeQuestionBlockParser()),
        new FirstNSelector(),
        _writer,
        new FixedTimeProvider(Now));

    private async Task AddSourceWithQuestions(int questionCount)
    {
        await _repository.AddAsync("/notes", Now);
        _fileSystem
            .AddDirectory("/notes")
            .AddFile("/notes/questions.md", string.Join('\n', Enumerable.Repeat("question", questionCount)));
    }

    [Fact]
    public async Task Creates_assessment_from_scanned_questions()
    {
        await AddSourceWithQuestions(3);

        CreateAssessmentResult result = await CreateService().CreateAsync(requestedCount: 3);

        Assert.Equal(CreateAssessmentStatus.Created, result.Status);
        Assert.Equal(3, result.QuestionCount);
        Assert.Equal("/output/Assessments/assessment-2026-07-16-001.md", result.FilePath);

        Assert.NotNull(_writer.Written);
        Assert.Equal("Assessment 2026-07-16", _writer.Written.Title);
        Assert.Equal(Now, _writer.Written.CreatedAtUtc);
        Assert.Equal(3, _writer.Written.Questions.Count);
    }

    [Fact]
    public async Task Uses_the_default_count_when_none_is_requested()
    {
        await AddSourceWithQuestions(25);

        CreateAssessmentResult result = await CreateService().CreateAsync();

        Assert.Equal(CreateAssessmentService.DefaultQuestionCount, result.QuestionCount);
    }

    [Fact]
    public async Task Takes_all_questions_when_fewer_exist_than_requested()
    {
        await AddSourceWithQuestions(2);

        CreateAssessmentResult result = await CreateService().CreateAsync(requestedCount: 10);

        Assert.Equal(2, result.QuestionCount);
    }

    [Fact]
    public async Task Reports_when_no_questions_are_found()
    {
        await _repository.AddAsync("/notes", Now);
        _fileSystem.AddDirectory("/notes");

        CreateAssessmentResult result = await CreateService().CreateAsync();

        Assert.Equal(CreateAssessmentStatus.NoQuestionsFound, result.Status);
        Assert.Null(result.FilePath);
        Assert.Null(_writer.Written);
    }

    [Fact]
    public async Task Rejects_count_below_one()
    {
        await AddSourceWithQuestions(3);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            CreateService().CreateAsync(requestedCount: 0));
    }
}
