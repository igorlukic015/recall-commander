namespace RecallCommander.Application.Artifacts;

/// <summary>
/// Default artifact writing pipeline: render → name → persist.
/// Generic over the artifact type so every artifact shares the same file
/// naming, output directory handling and persistence behavior.
/// </summary>
public sealed class ArtifactWriter<T>(
    IArtifactRenderer<T> renderer,
    IArtifactStore store,
    IArtifactOutputPathProvider outputPath,
    ArtifactFileNameGenerator fileNames,
    TimeProvider timeProvider) : IArtifactWriter<T>
{
    public async Task<SavedArtifact> WriteAsync(T artifact, CancellationToken cancellationToken = default)
    {
        var content = renderer.Render(artifact);
        var stem = fileNames.CreateStem(content.Slug, timeProvider.GetUtcNow());
        var directory = Path.Combine(outputPath.GetOutputDirectory(), content.DirectoryName);

        var filePath = await store.SaveAsync(directory, stem, content.Markdown, cancellationToken);
        return new SavedArtifact(filePath);
    }
}
