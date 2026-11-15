namespace RecallCommander.Application.Sources;

public enum RemoveSourceStatus
{
    Removed,
    NotRegistered,
}

public sealed record RemoveSourceResult(RemoveSourceStatus Status, string DirectoryPath);
