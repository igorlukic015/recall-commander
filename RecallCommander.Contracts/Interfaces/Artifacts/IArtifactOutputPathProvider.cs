namespace RecallCommander.Contracts.Artifacts;

/// <summary>
/// Decides where generated artifacts are written.
/// </summary>
public interface IArtifactOutputPathProvider
{
    string GetOutputDirectory();
}
