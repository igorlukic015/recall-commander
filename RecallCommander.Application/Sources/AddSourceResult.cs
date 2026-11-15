using RecallCommander.Domain;

namespace RecallCommander.Application.Sources;

public enum AddSourceStatus
{
    Added,
    AlreadyRegistered,
    DirectoryNotFound,
}

public sealed record AddSourceResult(AddSourceStatus Status, string DirectoryPath, QuestionSource? Source);
