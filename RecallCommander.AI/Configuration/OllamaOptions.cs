namespace RecallCommander.AI.Configuration;

/// <summary>
/// Ollama provider configuration ("Ai:Ollama" section). Ollama runs locally
/// and needs no API key.
/// </summary>
public sealed class OllamaOptions
{
    /// <summary>Base address of the Ollama server.</summary>
    public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>The model to run, e.g. "llama3.2". Must be configured.</summary>
    public string? Model { get; set; }
}
