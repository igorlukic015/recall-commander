namespace RecallCommander.Contracts.Artifacts;

/// <summary>
/// Persists rendered artifacts. Implementations assign the next free sequence
/// number for the given name stem and must never overwrite an existing file —
/// artifacts are historical records.
/// </summary>
public interface IArtifactStore
{
    /// <summary>
    /// Assigns the first available sequenced name for
    /// <paramref name="fileNameStem"/> (e.g. "assessment-2026-07-16" →
    /// "assessment-2026-07-16-001.md"), invokes
    /// <paramref name="renderMarkdown"/> with the resulting artifact id (the
    /// file name without extension) to obtain the document, writes it into
    /// the directory (creating it if needed) and returns the full path of the
    /// file actually written. Rendering happens inside the store so the id
    /// embedded in the document always matches the file name.
    /// </summary>
    Task<string> SaveAsync(
        string directoryPath,
        string fileNameStem,
        Func<string, string> renderMarkdown,
        CancellationToken cancellationToken = default);
}
