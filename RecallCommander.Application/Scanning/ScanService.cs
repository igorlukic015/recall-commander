using RecallCommander.Contracts.FileSystem;
using RecallCommander.Contracts.Questions;
using RecallCommander.Contracts.Sources;
using RecallCommander.Domain;

namespace RecallCommander.Application.Scanning;

/// <summary>
/// Scans every registered question source, parses all Markdown files, and
/// aggregates discovered questions and warnings. Nothing is persisted;
/// each scan reparses the sources from scratch.
/// </summary>
public sealed class ScanService(
    IQuestionSourceRepository repository,
    IFileSystem fileSystem,
    IQuestionBlockParser parser)
{
    public async Task<ScanReport> ScanAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<QuestionSource> sources = await repository.GetAllAsync(cancellationToken);

        List<ScannedFile> files = new List<ScannedFile>();
        List<ScanWarning> warnings = new List<ScanWarning>();
        HashSet<string> scannedPaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (QuestionSource source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!fileSystem.DirectoryExists(source.DirectoryPath))
            {
                warnings.Add(new ScanWarning(source.DirectoryPath, LineNumber: null, "Source directory not found."));
                continue;
            }

            foreach (string filePath in fileSystem.EnumerateMarkdownFiles(source.DirectoryPath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Overlapping sources must not produce duplicate questions.
                if (!scannedPaths.Add(filePath))
                {
                    continue;
                }

                string displayPath = Path.GetRelativePath(source.DirectoryPath, filePath);

                string markdown;
                try
                {
                    markdown = fileSystem.ReadAllText(filePath);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    warnings.Add(new ScanWarning(displayPath, LineNumber: null, $"Could not read file: {exception.Message}"));
                    continue;
                }

                QuestionParseResult result = parser.Parse(markdown);

                files.Add(new ScannedFile(displayPath, filePath, result.Questions));
                warnings.AddRange(result.Diagnostics.Select(diagnostic =>
                    new ScanWarning(displayPath, diagnostic.LineNumber, diagnostic.Message)));
            }
        }

        return new ScanReport(sources.Count, files, warnings);
    }
}
