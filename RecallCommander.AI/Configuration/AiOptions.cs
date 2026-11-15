namespace RecallCommander.AI.Configuration;

/// <summary>
/// AI configuration, bound from the "Ai" configuration section
/// (appsettings.json, user secrets, or a gitignored .env file with keys such
/// as Ai__Provider). The default provider is "fake", so nothing reaches the
/// network unless a real provider is configured explicitly.
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public const string FakeProvider = "fake";
    public const string OllamaProvider = "ollama";
    public const string GeminiProvider = "gemini";

    /// <summary>"fake", "ollama" or "gemini" (case-insensitive).</summary>
    public string Provider { get; set; } = FakeProvider;

    public OllamaOptions Ollama { get; set; } = new();

    public GeminiOptions Gemini { get; set; } = new();
}
