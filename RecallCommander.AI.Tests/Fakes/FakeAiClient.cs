using RecallCommander.AI.Clients;

namespace RecallCommander.AI.Tests.Fakes;

/// <summary>
/// A deterministic AI client for tests: never touches the network, returns
/// the predefined content and records every request it receives.
/// </summary>
public sealed class FakeAiClient(string content) : IAiClient
{
    public const string Provider = "fake-ai";
    public const string Model = "fake-model";

    public List<AiRequest> Requests { get; } = [];

    public string Name => $"{Provider}/{Model}";

    public Task<AiResponse> CompleteAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult(new AiResponse(content, Provider, Model));
    }
}
