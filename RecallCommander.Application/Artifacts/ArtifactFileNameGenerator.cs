using System.Text;

namespace RecallCommander.Application.Artifacts;

/// <summary>
/// Produces filesystem-safe artifact file names of the form
/// "assessment-2026-07-16-001.md": a sanitized slug, the creation date, and a
/// deterministic three-digit sequence number assigned by the artifact store.
/// </summary>
public sealed class ArtifactFileNameGenerator
{
    private const string FallbackSlug = "artifact";
    private const string Extension = ".md";

    /// <summary>Creates the date-stamped name stem, e.g. "assessment-2026-07-16".</summary>
    public string CreateStem(string slug, DateTimeOffset timestampUtc)
    {
        return $"{Sanitize(slug)}-{timestampUtc:yyyy-MM-dd}";
    }

    /// <summary>Appends the sequence number, e.g. "assessment-2026-07-16-001.md".</summary>
    public string CreateNumberedFileName(string stem, int sequence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stem);
        ArgumentOutOfRangeException.ThrowIfLessThan(sequence, 1);

        return $"{stem}-{sequence:000}{Extension}";
    }

    private static string Sanitize(string slug)
    {
        StringBuilder builder = new StringBuilder(slug.Length);
        bool previousWasDash = true; // suppresses leading dashes

        foreach (char character in slug.Trim().ToLowerInvariant())
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

        string sanitized = builder.ToString().TrimEnd('-');
        return sanitized.Length > 0 ? sanitized : FallbackSlug;
    }
}
