namespace RecallCommander.AI.Clients;

/// <summary>
/// A provider-independent completion response: the generated text plus which
/// provider and model produced it.
/// </summary>
public sealed record AiResponse(string Content, string Provider, string Model);
