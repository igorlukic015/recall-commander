using RecallCommander.Contracts.Artifacts;

namespace RecallCommander.Infrastructure.Artifacts;

/// <summary>
/// Writes artifacts to disk under deterministic sequenced names: the first
/// free "{stem}-NNN.md" slot, counting up from 001. Existing files are never
/// overwritten — artifacts are historical records.
/// </summary>
public sealed class ArtifactFileStore(ArtifactFileNameGenerator fileNames) : IArtifactStore
{
    public async Task<string> SaveAsync(
        string directoryPath,
        string fileNameStem,
        Func<string, string> renderMarkdown,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(directoryPath);

        string filePath = NextAvailablePath(directoryPath, fileNameStem);
        string artifactId = Path.GetFileNameWithoutExtension(filePath);

        await File.WriteAllTextAsync(filePath, renderMarkdown(artifactId), cancellationToken);
        return filePath;
    }

    private string NextAvailablePath(string directoryPath, string fileNameStem)
    {
        for (int sequence = 1; ; sequence++)
        {
            string candidate = Path.Combine(directoryPath, fileNames.CreateNumberedFileName(fileNameStem, sequence));
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }
}
