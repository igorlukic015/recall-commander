using RecallCommander.AI;
using RecallCommander.AI.Clients;
using RecallCommander.AI.Evaluation;
using RecallCommander.AI.Prompts;
using RecallCommander.Application.Artifacts;
using RecallCommander.Application.Attempts;
using RecallCommander.Application.Reviews;
using RecallCommander.Domain;
using RecallCommander.Infrastructure.Artifacts;
using RecallCommander.Infrastructure.FileSystem;
using RecallCommander.IntegrationTests.Support;
using RecallCommander.Markdown.Parsing;
using RecallCommander.Markdown.Writing;
using Xunit;

namespace RecallCommander.IntegrationTests.Integration;

/// <summary>
/// The review workflow through the real AI evaluation chain — prompt builder,
/// evaluator, response parser, renderer, artifact store — with only the AI
/// client faked. No network calls are made anywhere in the suite.
/// </summary>
public sealed class ReviewAiEvaluationTests : IDisposable
{
    private const string EvaluationJson =
        """
        {
          "score": 7,
          "level": "Good",
          "summary": "A solid answer with room to grow.",
          "strengths": ["Accurate core definition."],
          "missing_information": ["The role of the heap."],
          "incorrect_statements": [],
          "suggestions": ["Add a code example."]
        }
        """;

    private readonly TestWorkspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    private CreateReviewService CreateService(IAiClient client)
    {
        ArtifactFileNameGenerator fileNames = new ArtifactFileNameGenerator();
        FixedTimeProvider clock = new FixedTimeProvider(new DateTimeOffset(2026, 7, 19, 20, 0, 0, TimeSpan.Zero));

        return new CreateReviewService(
            new ValidateAttemptService(new PhysicalFileSystem(), new AttemptParser()),
            new AiQuestionEvaluator(
                client,
                new ReviewPromptBuilder(new PromptLoader()),
                new EvaluationResponseParser()),
            new ArtifactWriter<Review>(
                new ReviewRenderer(),
                new ArtifactFileStore(fileNames),
                new FixedArtifactOutputPathProvider(_workspace.Root),
                fileNames,
                clock),
            clock);
    }

    private string WriteAttempt() => _workspace.WriteFile(
        Path.Combine(_workspace.Root, "Attempts"),
        "assessment-2026-07-19-001.attempt.md",
        """
        ---
        type: assessment
        id: assessment-2026-07-19-001
        title: C# Assessment
        created: 2026-07-19T18:00:00
        ---

        # C# Assessment

        ---

        ## Question 1

        What is boxing in C#?

        ### Answer

        Boxing wraps a value type in a heap object.
        """);

    [Fact]
    public async Task Reviews_an_attempt_through_the_ai_chain_without_any_network()
    {
        RecordingAiClient client = new RecordingAiClient(EvaluationJson);

        CreateReviewResult result = await CreateService(client).CreateAsync(WriteAttempt());

        Assert.Equal(CreateReviewStatus.Created, result.Status);
        string document = await File.ReadAllTextAsync(result.ReviewFilePath!);

        // The AI client received the actual question and answer.
        AiRequest request = Assert.Single(client.Requests);
        Assert.Contains("What is boxing in C#?", request.UserPrompt);
        Assert.Contains("Boxing wraps a value type in a heap object.", request.UserPrompt);

        // The artifact records which evaluator produced it.
        Assert.Contains("evaluator: test-provider/test-model", document);
        Assert.Contains("attempt: assessment-2026-07-19-001", document);

        // The model's evaluation landed in the artifact.
        Assert.Contains("Score: 7/10", document);
        Assert.Contains("Understanding: Good", document);
        Assert.Contains("A solid answer with room to grow.", document);
        Assert.Contains("- Accurate core definition.", document);
        Assert.Contains("- The role of the heap.", document);
        Assert.Contains("- Add a code example.", document);
    }

    [Fact]
    public async Task An_unusable_model_reply_fails_the_review_and_writes_nothing()
    {
        RecordingAiClient client = new RecordingAiClient("I am not JSON.");
        CreateReviewService service = CreateService(client);
        string attemptPath = WriteAttempt();

        await Assert.ThrowsAsync<AiException>(() => service.CreateAsync(attemptPath));

        Assert.False(Directory.Exists(_workspace.ReviewsDirectory));
    }

    private sealed class RecordingAiClient(string content) : IAiClient
    {
        public List<AiRequest> Requests { get; } = [];

        public string Name => "test-provider/test-model";

        public Task<AiResponse> CompleteAsync(AiRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(new AiResponse(content, "test-provider", "test-model"));
        }
    }
}
