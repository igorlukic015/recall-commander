using RecallCommander.Application.Attempts;
using RecallCommander.Contracts.Artifacts;
using RecallCommander.Contracts.Parsing;
using RecallCommander.Contracts.Reviews;
using RecallCommander.Domain;

namespace RecallCommander.Application.Reviews;

/// <summary>
/// Creates a review artifact for a completed attempt: reads and parses the
/// attempt file, evaluates every question through
/// <see cref="IQuestionEvaluator"/>, assembles the <see cref="Review"/>
/// snapshot and writes the Markdown file. The attempt file stays untouched.
/// </summary>
public sealed class CreateReviewService(
    ValidateAttemptService attempts,
    IQuestionEvaluator evaluator,
    IArtifactWriter<Review> writer,
    TimeProvider timeProvider)
{
    public async Task<CreateReviewResult> CreateAsync(
        string attemptPath,
        CancellationToken cancellationToken = default)
    {
        ValidateAttemptResult validation = attempts.Validate(attemptPath);

        switch (validation.Status)
        {
            case ValidateAttemptStatus.FileNotFound:
                return CreateReviewResult.FileNotFound(validation.FilePath);

            case ValidateAttemptStatus.Invalid:
                return CreateReviewResult.InvalidAttempt(validation.FilePath, validation.Diagnostics);
        }

        Attempt attempt = validation.Attempt!;

        List<QuestionReview> questionReviews = new List<QuestionReview>();
        foreach (AttemptQuestion question in attempt.Questions)
        {
            ReviewEvaluation evaluation = await evaluator.EvaluateAsync(
                question.Prompt,
                question.Answer,
                cancellationToken);

            questionReviews.Add(new QuestionReview(question.Prompt, question.Answer, evaluation));
        }

        int answered = attempt.Questions.Count(question => question.IsAnswered);
        Review review = new Review(
            $"Review - {attempt.Title}",
            timeProvider.GetUtcNow(),
            $"{answered} of {attempt.Questions.Count} questions were answered.",
            questionReviews,
            attempt.AssessmentId,
            evaluator.Name);

        SavedArtifact saved = await writer.WriteAsync(review, cancellationToken);
        return CreateReviewResult.Created(validation.FilePath, saved.FilePath, review.QuestionReviews.Count);
    }
}

public enum CreateReviewStatus
{
    Created,
    InvalidAttempt,
    FileNotFound,
}

public sealed record CreateReviewResult(
    CreateReviewStatus Status,
    string AttemptFilePath,
    string? ReviewFilePath,
    int QuestionCount,
    IReadOnlyList<ParseDiagnostic> Diagnostics)
{
    public static CreateReviewResult Created(string attemptFilePath, string reviewFilePath, int questionCount) =>
        new(CreateReviewStatus.Created, attemptFilePath, reviewFilePath, questionCount, []);

    public static CreateReviewResult InvalidAttempt(string attemptFilePath, IReadOnlyList<ParseDiagnostic> diagnostics) =>
        new(CreateReviewStatus.InvalidAttempt, attemptFilePath, ReviewFilePath: null, QuestionCount: 0, diagnostics);

    public static CreateReviewResult FileNotFound(string attemptFilePath) =>
        new(CreateReviewStatus.FileNotFound, attemptFilePath, ReviewFilePath: null, QuestionCount: 0, []);
}
