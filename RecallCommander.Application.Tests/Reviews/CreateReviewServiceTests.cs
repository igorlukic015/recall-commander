using RecallCommander.Application.Attempts;
using RecallCommander.Application.Reviews;
using RecallCommander.Application.Tests.Fakes;
using RecallCommander.Contracts.Artifacts;
using RecallCommander.Contracts.Attempts;
using RecallCommander.Contracts.Parsing;
using RecallCommander.Contracts.Reviews;
using RecallCommander.Domain;
using Xunit;

namespace RecallCommander.Application.Tests.Reviews;

public sealed class CreateReviewServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 19, 20, 0, 0, TimeSpan.Zero);

    private readonly RecordingWriter _writer = new();
    private readonly RecordingEvaluator _evaluator = new();

    private sealed class RecordingEvaluator : IQuestionEvaluator
    {
        public List<(string Prompt, string Answer)> Calls { get; } = [];

        public string Name => "recording";

        public Task<ReviewEvaluation> EvaluateAsync(
            string prompt,
            string answer,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((prompt, answer));
            return Task.FromResult(new ReviewEvaluation(
                7, UnderstandingLevel.Good, "Evaluated.", [], [], [], []));
        }
    }

    private sealed class RecordingWriter : IArtifactWriter<Review>
    {
        public Review? Written { get; private set; }

        public Task<SavedArtifact> WriteAsync(Review artifact, CancellationToken cancellationToken = default)
        {
            Written = artifact;
            return Task.FromResult(new SavedArtifact("/output/Reviews/review-2026-07-19-001.md"));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static Attempt SampleAttempt() => new(
        "C# Assessment",
        new DateTimeOffset(2026, 7, 17, 18, 0, 0, TimeSpan.Zero),
        [
            new AttemptQuestion("What is boxing?", "Boxing wraps a value type in an object."),
            new AttemptQuestion("Explain garbage collection.", ""),
        ],
        "assessment-2026-07-17-001");

    private CreateReviewService CreateService(AttemptParseResult parseResult, FakeFileSystem fileSystem) => new(
        new ValidateAttemptService(fileSystem, new FakeAttemptParser(parseResult)),
        _evaluator,
        _writer,
        new FixedTimeProvider(Now));

    [Fact]
    public async Task Creates_a_review_evaluating_every_attempt_question()
    {
        FakeFileSystem fileSystem = new FakeFileSystem().AddFile("attempts/done.md", "the document");
        CreateReviewService service = CreateService(new AttemptParseResult(SampleAttempt(), []), fileSystem);

        CreateReviewResult result = await service.CreateAsync("attempts/done.md");

        Assert.Equal(CreateReviewStatus.Created, result.Status);
        Assert.Equal("attempts/done.md", result.AttemptFilePath);
        Assert.Equal("/output/Reviews/review-2026-07-19-001.md", result.ReviewFilePath);
        Assert.Equal(2, result.QuestionCount);

        Assert.Equal(
            [
                ("What is boxing?", "Boxing wraps a value type in an object."),
                ("Explain garbage collection.", ""),
            ],
            _evaluator.Calls);

        Assert.NotNull(_writer.Written);
        Assert.Equal("Review - C# Assessment", _writer.Written.Title);
        Assert.Equal(Now, _writer.Written.CreatedAtUtc);
        Assert.Equal("assessment-2026-07-17-001", _writer.Written.AttemptId);
        Assert.Equal("recording", _writer.Written.Evaluator);
        Assert.Equal("1 of 2 questions were answered.", _writer.Written.OverallSummary);

        Assert.Equal(2, _writer.Written.QuestionReviews.Count);
        Assert.Equal("What is boxing?", _writer.Written.QuestionReviews[0].Prompt);
        Assert.Equal("Boxing wraps a value type in an object.", _writer.Written.QuestionReviews[0].Answer);
        Assert.Equal(7, _writer.Written.QuestionReviews[0].Evaluation.Score);
        Assert.Equal("", _writer.Written.QuestionReviews[1].Answer);
    }

    [Fact]
    public async Task Reports_a_missing_file_without_writing_anything()
    {
        CreateReviewService service = CreateService(
            new AttemptParseResult(SampleAttempt(), []),
            new FakeFileSystem());

        CreateReviewResult result = await service.CreateAsync("attempts/missing.md");

        Assert.Equal(CreateReviewStatus.FileNotFound, result.Status);
        Assert.Equal("attempts/missing.md", result.AttemptFilePath);
        Assert.Null(result.ReviewFilePath);
        Assert.Empty(_evaluator.Calls);
        Assert.Null(_writer.Written);
    }

    [Fact]
    public async Task Passes_diagnostics_through_for_an_invalid_attempt()
    {
        ParseDiagnostic[] diagnostics = [new ParseDiagnostic(7, "Missing '### Answer' heading.")];
        FakeFileSystem fileSystem = new FakeFileSystem().AddFile("attempts/broken.md", "the document");
        CreateReviewService service = CreateService(
            new AttemptParseResult(Attempt: null, diagnostics),
            fileSystem);

        CreateReviewResult result = await service.CreateAsync("attempts/broken.md");

        Assert.Equal(CreateReviewStatus.InvalidAttempt, result.Status);
        Assert.Equal(diagnostics, result.Diagnostics);
        Assert.Null(result.ReviewFilePath);
        Assert.Empty(_evaluator.Calls);
        Assert.Null(_writer.Written);
    }
}
