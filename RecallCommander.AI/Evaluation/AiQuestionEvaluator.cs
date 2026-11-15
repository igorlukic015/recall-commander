using RecallCommander.AI.Clients;
using RecallCommander.Contracts.Reviews;
using RecallCommander.Domain;

namespace RecallCommander.AI.Evaluation;

/// <summary>
/// Evaluates a question and answer through an AI provider: builds the review
/// prompts, sends them through <see cref="IAiClient"/> and parses the model's
/// JSON reply into a <see cref="ReviewEvaluation"/>. Which provider answers
/// is configuration; nothing above this class knows.
/// </summary>
public sealed class AiQuestionEvaluator(
    IAiClient client,
    ReviewPromptBuilder prompts,
    EvaluationResponseParser parser) : IQuestionEvaluator
{
    public string Name => client.Name;

    public async Task<ReviewEvaluation> EvaluateAsync(
        string prompt,
        string answer,
        CancellationToken cancellationToken = default)
    {
        AiRequest request = prompts.Build(prompt, answer);
        AiResponse response = await client.CompleteAsync(request, cancellationToken);
        return parser.Parse(response.Content);
    }
}
