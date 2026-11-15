using RecallCommander.AI.Clients;
using RecallCommander.AI.Prompts;

namespace RecallCommander.AI.Evaluation;

/// <summary>
/// Builds the <see cref="AiRequest"/> for evaluating one question: loads the
/// review prompts and injects the question text and the user's answer into
/// the user prompt template.
/// </summary>
public sealed class ReviewPromptBuilder(PromptLoader prompts)
{
    private const string SystemPromptPath = "Review/SystemPrompt.md";
    private const string UserPromptPath = "Review/UserPrompt.md";

    private const string QuestionPlaceholder = "{{question}}";
    private const string AnswerPlaceholder = "{{answer}}";

    /// <summary>An empty answer means the question was left unanswered; the
    /// system prompt instructs the model how to treat it.</summary>
    public AiRequest Build(string question, string answer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentNullException.ThrowIfNull(answer);

        string userPrompt = prompts.Load(UserPromptPath)
            .Replace(QuestionPlaceholder, question.Trim(), StringComparison.Ordinal)
            .Replace(AnswerPlaceholder, answer.Trim(), StringComparison.Ordinal);

        return new AiRequest(prompts.Load(SystemPromptPath), userPrompt);
    }
}
