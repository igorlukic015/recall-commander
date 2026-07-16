namespace RecallCommander.Application.Artifacts;

/// <summary>
/// Persists rendered artifacts. Implementations assign the next free sequence
/// number for the given name stem and must never overwrite an existing file —
/// artifacts are historical records.
/// </summary>
public interface IArtifactStore
{
    /// <summary>
    /// Writes the document into the directory (creating it if needed) under
    /// the first available sequenced name for <paramref name="fileNameStem"/>
    /// (e.g. "assessment-2026-07-16" → "assessment-2026-07-16-001.md") and
    /// returns the full path of the file actually written.
    /// </summary>
    Task<string> SaveAsync(
        string directoryPath,
        string fileNameStem,
        string markdown,
        CancellationToken cancellationToken = default);
}