namespace RecallCommander.Application.Artifacts;

/// <summary>
/// Persists rendered artifacts. Implementations must never overwrite an
/// existing file — artifacts are historical records.
/// </summary>
public interface IArtifactStore
{
    /// <summary>
    /// Writes the document into the directory (creating it if needed) and
    /// returns the full path of the file actually written.
    /// </summary>
    Task<string> SaveAsync(
        string directoryPath,
        string fileName,
        string markdown,
        CancellationToken cancellationToken = default);
}
