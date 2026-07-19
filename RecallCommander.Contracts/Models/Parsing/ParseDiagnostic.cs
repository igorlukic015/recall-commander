namespace RecallCommander.Contracts.Parsing;

/// <summary>A problem found while parsing, anchored to a 1-based line number.</summary>
public sealed record ParseDiagnostic(int LineNumber, string Message);
