namespace RecallCommander.AI.Clients;

/// <summary>
/// Communicates with one model provider. Implementations own transport,
/// serialization and authentication; callers only see prompts in and
/// generated text out.
/// </summary>
public interface IAiClient
{
    /// <summary>
    /// Identity of the provider and model this client talks to
    /// (e.g. "ollama/llama3.2"), recorded in review artifacts.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Sends the request to the provider and returns the completion.
    /// Throws <see cref="AiException"/> when the provider cannot be reached
    /// or returns an error.
    /// </summary>
    Task<AiResponse> CompleteAsync(AiRequest request, CancellationToken cancellationToken = default);
}
