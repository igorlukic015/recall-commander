namespace RecallCommander.Contracts.Workspace;

/// <summary>
/// Creates the application metadata store. Safe to call repeatedly.
/// </summary>
public interface IWorkspaceInitializer
{
    Task<InitializationResult> InitializeAsync(CancellationToken cancellationToken = default);
}
