namespace RecallCommander.Contracts.Artifacts;

/// <summary>
/// Renders an artifact to Markdown and persists it as a new file.
/// One writer exists per artifact type; registering an
/// <see cref="IArtifactRenderer{T}"/> is all a new artifact type needs.
/// </summary>
public interface IArtifactWriter<in T>
{
    Task<SavedArtifact> WriteAsync(T artifact, CancellationToken cancellationToken = default);
}
