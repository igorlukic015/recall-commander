using System.Diagnostics.CodeAnalysis;
using RecallCommander.Contracts.Parsing;
using RecallCommander.Domain;

namespace RecallCommander.Contracts.Attempts;

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
