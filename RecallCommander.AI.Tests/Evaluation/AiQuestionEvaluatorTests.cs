using RecallCommander.AI.Clients;
using RecallCommander.AI.Evaluation;
using RecallCommander.AI.Prompts;
using RecallCommander.AI.Tests.Fakes;
using RecallCommander.Domain;
using Xunit;

namespace RecallCommander.AI.Tests.Evaluation;

/// <summary>
/// The full evaluation chain — prompt builder, AI client, response parser —
/// with only the client faked. No network is involved.
/// </summary>
public sealed class AiQuestionEvaluatorTests
{
    private const string ValidResponse =
        """
        {
          "score": 8,
          "level": "Strong",
          "summary": "Good understanding",
          "strengths": ["Accurate definition."],
          "missing_information": [],
          "incorrect_statements": [],
          "suggestions": ["Add an example."]
        }
        """;

    private static AiQuestionEvaluator CreateEvaluator(IAiClient client) => new(
        client,
        new ReviewPromptBuilder(new PromptLoader()),
        new EvaluationResponseParser());

    [Fact]
    public async Task Evaluates_a_question_through_the_client()
    {
        FakeAiClient client = new FakeAiClient(ValidResponse);

        ReviewEvaluation evaluation = await CreateEvaluator(client).EvaluateAsync(
            "What is boxing?",
            "Boxing wraps a value type in an object.");

        Assert.Equal(8, evaluation.Score);
        Assert.Equal(UnderstandingLevel.Strong, evaluation.Understanding);
        Assert.Equal("Good understanding", evaluation.Summary);
        Assert.Equal(["Accurate definition."], evaluation.Strengths);
        Assert.Equal(["Add an example."], evaluation.Suggestions);

        AiRequest request = Assert.Single(client.Requests);
        Assert.Contains("What is boxing?", request.UserPrompt);
        Assert.Contains("Boxing wraps a value type in an object.", request.UserPrompt);
        Assert.Contains("\"score\"", request.SystemPrompt);
    }

    [Fact]
    public void The_evaluator_name_is_the_client_name()
    {
        Assert.Equal("fake-ai/fake-model", CreateEvaluator(new FakeAiClient(ValidResponse)).Name);
    }

    [Fact]
    public async Task An_unusable_response_surfaces_as_an_ai_error()
    {
        AiQuestionEvaluator evaluator = CreateEvaluator(new FakeAiClient("I refuse to answer."));

        await Assert.ThrowsAsync<AiException>(() => evaluator.EvaluateAsync("Prompt?", "Answer."));
    }

    [Fact]
    public async Task A_client_failure_propagates()
    {
        AiQuestionEvaluator evaluator = CreateEvaluator(new ThrowingAiClient());

        AiException exception = await Assert.ThrowsAsync<AiException>(() =>
            evaluator.EvaluateAsync("Prompt?", "Answer."));

        Assert.Contains("unreachable", exception.Message);
    }

    private sealed class ThrowingAiClient : IAiClient
    {
        public string Name => "throwing/none";

        public Task<AiResponse> CompleteAsync(AiRequest request, CancellationToken cancellationToken = default) =>
            throw new AiException("The provider is unreachable.");
    }
}
