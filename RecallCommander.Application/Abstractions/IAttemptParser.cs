using System.Diagnostics.CodeAnalysis;
using RecallCommander.Domain;

namespace RecallCommander.Application.Abstractions;

/// <summary>
/// Parses a completed assessment Markdown document into an <see cref="Attempt"/>.
/// Problems are reported as diagnostics; parsing always continues so every
/// problem in the document is reported at once.
/// </summary>
public interface IAttemptParser
{
    AttemptParseResult Parse(string markdown);
}

/// <summary>
/// The outcome of parsing one attempt document. The attempt is only present
/// when the document had no errors; a partially parsed attempt would silently
/// misrepresent what the user answered.
/// </summary>
public sealed record AttemptParseResult(
    Attempt? Attempt,
    IReadOnlyList<ParseDiagnostic> Diagnostics)
{
    [MemberNotNullWhen(true, nameof(Attempt))]
    public bool IsValid => Attempt is not null;
}
