using RecallCommander.Application.Artifacts;
using RecallCommander.Application.Scanning;
using RecallCommander.Contracts.Artifacts;
using RecallCommander.Contracts.Assessments;
using RecallCommander.Domain;

namespace RecallCommander.Application.Assessments;

/// <summary>
/// Creates an assessment artifact: scans all question sources, selects
/// questions, snapshots them into an <see cref="Assessment"/> and writes the
/// Markdown file. Nothing is persisted to the database; the file is the
/// artifact.
/// </summary>
public sealed class CreateAssessmentService(
    ScanService scanner,
    IQuestionSelector selector,
    IArtifactWriter<Assessment> writer,
    TimeProvider timeProvider)
{
    public const int DefaultQuestionCount = 10;

    public async Task<CreateAssessmentResult> CreateAsync(
        int? requestedCount = null,
        CancellationToken cancellationToken = default)
    {
        int count = requestedCount ?? DefaultQuestionCount;
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1, nameof(requestedCount));

        ScanReport report = await scanner.ScanAsync(cancellationToken);
        List<Question> questions = report.Files
            .SelectMany(file => file.Questions)
            .Select(discovered => discovered.Question)
            .ToList();

        if (questions.Count == 0)
        {
            return CreateAssessmentResult.NoQuestions();
        }

        IReadOnlyList<Question> selected = selector.Select(questions, count);
        DateTimeOffset createdAt = timeProvider.GetUtcNow();

        Assessment assessment = new Assessment(
            $"Assessment {createdAt:yyyy-MM-dd}",
            createdAt,
            selected.Select(AssessmentQuestion.FromQuestion));

        SavedArtifact saved = await writer.WriteAsync(assessment, cancellationToken);
        return CreateAssessmentResult.Created(saved.FilePath, assessment.Questions.Count);
    }
}

public enum CreateAssessmentStatus
{
    Created,
    NoQuestionsFound,
}

public sealed record CreateAssessmentResult(
    CreateAssessmentStatus Status,
    string? FilePath,
    int QuestionCount)
{
    public static CreateAssessmentResult Created(string filePath, int questionCount) =>
        new(CreateAssessmentStatus.Created, filePath, questionCount);

    public static CreateAssessmentResult NoQuestions() =>
        new(CreateAssessmentStatus.NoQuestionsFound, FilePath: null, QuestionCount: 0);
}
