namespace RecallCommander.Contracts.Exceptions;

/// <summary>
/// Thrown when a workflow requires the metadata store but 'rc init' has not been run.
/// </summary>
public sealed class WorkspaceNotInitializedException()
    : Exception("Workspace is not initialized. Run 'rc init' first.");
