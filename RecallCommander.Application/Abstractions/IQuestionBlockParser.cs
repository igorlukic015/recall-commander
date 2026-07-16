using RecallCommander.Domain;

namespace RecallCommander.Application.Abstractions;

/// <summary>
/// Extracts questions from the Question Blocks of a single Markdown document.
/// Invalid blocks are reported as diagnostics; parsing always continues.
/// </summary>
public interface IQuestionBlockParser
{
    QuestionParseResult Parse(string markdown);
}

/// <summary>The outcome of parsing one Markdown document.</summary>
public sealed record QuestionParseResult(
    IReadOnlyList<DiscoveredQuestion> Questions,
    IReadOnlyList<ParseDiagnostic> Diagnostics);

/// <summary>A question together with the line its Question Block starts on.</summary>
public sealed record DiscoveredQuestion(Question Question, int LineNumber);

/// <summary>A problem found while parsing, anchored to a 1-based line number.</summary>
public sealed record ParseDiagnostic(int LineNumber, string Message);
