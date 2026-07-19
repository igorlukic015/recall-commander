using System.Reflection;

namespace RecallCommander.AI.Prompts;

/// <summary>
/// Loads prompt Markdown files embedded in this assembly under Prompts/
/// (e.g. "Review/SystemPrompt.md"). Prompts ship inside the binary so the
/// tool works from any working directory.
/// </summary>
public sealed class PromptLoader
{
    private const string ResourcePrefix = "RecallCommander.AI.Prompts.";

    private static readonly Assembly PromptAssembly = typeof(PromptLoader).Assembly;

    public string Load(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        string resourceName = ResourcePrefix + relativePath.Replace('/', '.');
        using Stream? stream = PromptAssembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            throw new AiException($"Prompt '{relativePath}' is not embedded in the AI assembly.");
        }

        using StreamReader reader = new StreamReader(stream);
        return reader.ReadToEnd().Trim();
    }
}
