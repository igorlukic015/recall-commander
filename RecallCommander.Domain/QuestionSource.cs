namespace RecallCommander.Domain;

/// <summary>
/// A directory registered by the user as a source of Markdown question files.
/// Sources are scanned recursively; their internal organization is the user's choice.
/// </summary>
public sealed class QuestionSource
{
    public long Id { get; }

    /// <summary>Absolute path of the registered directory.</summary>
    public string DirectoryPath { get; }

    public DateTimeOffset RegisteredAtUtc { get; }

    public QuestionSource(long id, string directoryPath, DateTimeOffset registeredAtUtc)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new DomainException("A question source path cannot be empty.");
        }

        Id = id;
        DirectoryPath = directoryPath;
        RegisteredAtUtc = registeredAtUtc;
    }
}
