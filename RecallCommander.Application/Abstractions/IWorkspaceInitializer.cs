namespace RecallCommander.Application.Abstractions;

/// <summary>
/// Creates the application metadata store. Safe to call repeatedly.
/// </summary>
public interface IWorkspaceInitializer
{
    Task<InitializationResult> InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed record InitializationResult(bool Created, string DatabasePath);
