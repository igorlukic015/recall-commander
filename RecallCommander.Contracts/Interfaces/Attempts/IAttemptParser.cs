using RecallCommander.Domain;

namespace RecallCommander.Contracts.Attempts;

/// <summary>
/// Parses a completed assessment Markdown document into an <see cref="Attempt"/>.
/// Problems are reported as diagnostics; parsing always continues so every
/// problem in the document is reported at once.
/// </summary>
public interface IAttemptParser
{
    AttemptParseResult Parse(string markdown);
}
