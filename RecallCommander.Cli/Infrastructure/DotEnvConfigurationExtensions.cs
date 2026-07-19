using Microsoft.Extensions.Configuration;

namespace RecallCommander.Cli.Infrastructure;

/// <summary>
/// Loads configuration from a .env file in the working directory: KEY=VALUE
/// lines, '#' comments, optional quotes, and "__" as the section separator
/// (Ai__Gemini__ApiKey → Ai:Gemini:ApiKey). The file is gitignored — local
/// settings and API keys live there instead of committed files or process
/// environment variables.
/// </summary>
public static class DotEnvConfigurationExtensions
{
    public static IConfigurationBuilder AddDotEnvFile(this IConfigurationBuilder builder, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return File.Exists(path)
            ? builder.AddInMemoryCollection(Parse(File.ReadAllLines(path)))
            : builder;
    }

    /// <summary>Malformed lines are skipped, never fatal — a broken .env must not take down every command.</summary>
    public static Dictionary<string, string?> Parse(IEnumerable<string> lines)
    {
        Dictionary<string, string?> values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.Ordinal))
            {
                line = line["export ".Length..].TrimStart();
            }

            int separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            string key = line[..separator].Trim();
            string value = Unquote(line[(separator + 1)..].Trim());

            values[key.Replace("__", ":", StringComparison.Ordinal)] = value;
        }

        return values;
    }

    private static string Unquote(string value) =>
        value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\''))
            ? value[1..^1]
            : value;
}
