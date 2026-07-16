using System.Text;

namespace RecallCommander.Application.Artifacts;

/// <summary>
/// Produces filesystem-safe, timestamped artifact file names such as
/// "csharp-internals-assessment-20260716-193000.md".
/// </summary>
public sealed class ArtifactFileNameGenerator
{
    private const string FallbackSlug = "artifact";
    private const string Extension = ".md";

    public string Create(string slug, DateTimeOffset timestampUtc)
    {
        return $"{Sanitize(slug)}-{timestampUtc:yyyyMMdd-HHmmss}{Extension}";
    }

    private static string Sanitize(string slug)
    {
        var builder = new StringBuilder(slug.Length);
        var previousWasDash = true; // suppresses leading dashes

        foreach (var character in slug.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasDash = false;
            }
            else if (!previousWasDash && (char.IsWhiteSpace(character) || character is '-' or '_'))
            {
                builder.Append('-');
                previousWasDash = true;
            }
        }

        var sanitized = builder.ToString().TrimEnd('-');
        return sanitized.Length > 0 ? sanitized : FallbackSlug;
    }
}
