using RecallCommander.Application.Artifacts;

namespace RecallCommander.Infrastructure.Artifacts;

/// <summary>
/// Writes artifacts to disk. Existing files are never overwritten — artifacts
/// are historical records — so name collisions get a numeric suffix.
/// </summary>
public sealed class ArtifactFileStore : IArtifactStore
{
    public async Task<string> SaveAsync(
        string directoryPath,
        string fileName,
        string markdown,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(directoryPath);

        var filePath = MakeUnique(Path.Combine(directoryPath, fileName));
        await File.WriteAllTextAsync(filePath, markdown, cancellationToken);
        return filePath;
    }

    private static string MakeUnique(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return filePath;
        }

        var directory = Path.GetDirectoryName(filePath)!;
        var stem = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);

        for (var suffix = 2; ; suffix++)
        {
            var candidate = Path.Combine(directory, $"{stem}-{suffix}{extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }
}
