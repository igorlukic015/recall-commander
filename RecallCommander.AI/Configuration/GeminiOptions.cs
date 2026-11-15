namespace RecallCommander.AI.Configuration;

/// <summary>
/// Gemini provider configuration ("Ai:Gemini" section). The API key is a
/// secret: supply it through user secrets or the gitignored .env file
/// (Ai__Gemini__ApiKey=...), never through committed files.
/// </summary>
public sealed class GeminiOptions
{
    /// <summary>Base address of the Gemini REST API.</summary>
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

    /// <summary>The model to run, e.g. "gemini-2.0-flash". Must be configured.</summary>
    public string? Model { get; set; }

    /// <summary>The API key. Must be configured; never store it in source or committed files.</summary>
    public string? ApiKey { get; set; }
}
