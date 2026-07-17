using RecallCommander.Application.Abstractions;
using RecallCommander.Domain;

namespace RecallCommander.Application.Attempts;

/// <summary>
/// Reads a user-completed assessment file and parses it into an
/// <see cref="Attempt"/>. The file stays untouched — attempts are authored
/// by the user and only ever read by Recall Commander.
/// </summary>
public sealed class ValidateAttemptService(IFileSystem fileSystem, IAttemptParser parser)
{
    public ValidateAttemptResult Validate(string path)
    {
        var filePath = fileSystem.NormalizePath(path);

        if (!fileSystem.FileExists(filePath))
        {
            return ValidateAttemptResult.FileNotFound(filePath);
        }

        var result = parser.Parse(fileSystem.ReadAllText(filePath));

        return result.IsValid
            ? ValidateAttemptResult.Valid(filePath, result.Attempt)
            : ValidateAttemptResult.Invalid(filePath, result.Diagnostics);
    }
}

public enum ValidateAttemptStatus
{
    Valid,
    Invalid,
    FileNotFound,
}

public sealed record ValidateAttemptResult(
    ValidateAttemptStatus Status,
    string FilePath,
    Attempt? Attempt,
    IReadOnlyList<ParseDiagnostic> Diagnostics)
{
    public static ValidateAttemptResult Valid(string filePath, Attempt attempt) =>
        new(ValidateAttemptStatus.Valid, filePath, attempt, []);

    public static ValidateAttemptResult Invalid(string filePath, IReadOnlyList<ParseDiagnostic> diagnostics) =>
        new(ValidateAttemptStatus.Invalid, filePath, Attempt: null, diagnostics);

    public static ValidateAttemptResult FileNotFound(string filePath) =>
        new(ValidateAttemptStatus.FileNotFound, filePath, Attempt: null, []);
}
