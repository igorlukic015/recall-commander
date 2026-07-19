namespace RecallCommander.AI.Clients;

/// <summary>
/// A provider-independent completion request: instructions for the model and
/// the content to respond to. Which model runs it is the provider's
/// configuration, not part of the request.
/// </summary>
public sealed record AiRequest
{
    public string SystemPrompt { get; }

    public string UserPrompt { get; }

    public AiRequest(string systemPrompt, string userPrompt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemPrompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(userPrompt);

        SystemPrompt = systemPrompt;
        UserPrompt = userPrompt;
    }
}
