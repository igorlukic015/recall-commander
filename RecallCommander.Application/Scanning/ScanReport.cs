using RecallCommander.Application.Abstractions;

namespace RecallCommander.Application.Scanning;

/// <summary>The aggregated result of scanning every registered question source.</summary>
public sealed record ScanReport(
    int SourceCount,
    IReadOnlyList<ScannedFile> Files,
    IReadOnlyList<ScanWarning> Warnings)
{
    public int TotalQuestions => Files.Sum(file => file.Questions.Count);
}

/// <summary>One Markdown file that was scanned.</summary>
/// <param name="DisplayPath">Path relative to the source directory, for display.</param>
/// <param name="FullPath">Absolute path of the file.</param>
public sealed record ScannedFile(
    string DisplayPath,
    string FullPath,
    IReadOnlyList<DiscoveredQuestion> Questions);

/// <summary>A problem encountered during a scan.</summary>
/// <param name="LineNumber">1-based line number, or null for file- and source-level warnings.</param>
public sealed record ScanWarning(string DisplayPath, int? LineNumber, string Message)
{
    public string Location => LineNumber is null ? DisplayPath : $"{DisplayPath}:{LineNumber}";
}
