using RecallCommander.Contracts.Artifacts;

namespace RecallCommander.Application.Artifacts;

/// <summary>
/// Default artifact writing pipeline: name → render → persist.
/// Generic over the artifact type so every artifact shares the same file
/// naming, output directory handling and persistence behavior. The store
/// assigns the artifact id (the sequenced file name) and rendering receives
/// it, so the id embedded in the document always matches the file name.
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
        var stem = fileNames.CreateStem(renderer.Slug, timeProvider.GetUtcNow());
        var directory = Path.Combine(outputPath.GetOutputDirectory(), renderer.DirectoryName);

        var filePath = await store.SaveAsync(
            directory,
            stem,
            artifactId => renderer.Render(artifact, artifactId),
            cancellationToken);

        return new SavedArtifact(filePath);
    }
}
