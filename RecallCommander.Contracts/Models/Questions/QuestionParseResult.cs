using RecallCommander.Contracts.Parsing;
using RecallCommander.Domain;

namespace RecallCommander.Contracts.Questions;

/// <summary>The outcome of parsing one Markdown document.</summary>
public sealed record QuestionParseResult(
    IReadOnlyList<DiscoveredQuestion> Questions,
    IReadOnlyList<ParseDiagnostic> Diagnostics);

/// <summary>A question together with the line its Question Block starts on.</summary>
public sealed record DiscoveredQuestion(Question Question, int LineNumber);
