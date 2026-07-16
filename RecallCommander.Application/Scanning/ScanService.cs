using RecallCommander.Application.Abstractions;

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
        var sources = await repository.GetAllAsync(cancellationToken);

        var files = new List<ScannedFile>();
        var warnings = new List<ScanWarning>();
        var scannedPaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!fileSystem.DirectoryExists(source.DirectoryPath))
            {
                warnings.Add(new ScanWarning(source.DirectoryPath, LineNumber: null, "Source directory not found."));
                continue;
            }

            foreach (var filePath in fileSystem.EnumerateMarkdownFiles(source.DirectoryPath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Overlapping sources must not produce duplicate questions.
                if (!scannedPaths.Add(filePath))
                {
                    continue;
                }

                var displayPath = Path.GetRelativePath(source.DirectoryPath, filePath);

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

                var result = parser.Parse(markdown);

                files.Add(new ScannedFile(displayPath, filePath, result.Questions));
                warnings.AddRange(result.Diagnostics.Select(diagnostic =>
                    new ScanWarning(displayPath, diagnostic.LineNumber, diagnostic.Message)));
            }
        }

        return new ScanReport(sources.Count, files, warnings);
    }
}
