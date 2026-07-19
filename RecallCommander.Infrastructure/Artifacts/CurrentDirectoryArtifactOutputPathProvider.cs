using RecallCommander.Contracts.Artifacts;

namespace RecallCommander.Infrastructure.Artifacts;

/// <summary>
/// Writes artifacts into the directory the CLI is invoked from. A configurable
/// output directory can replace this implementation later without touching
/// any artifact writer.
/// </summary>
public sealed class CurrentDirectoryArtifactOutputPathProvider : IArtifactOutputPathProvider
{
    public string GetOutputDirectory() => Environment.CurrentDirectory;
}
