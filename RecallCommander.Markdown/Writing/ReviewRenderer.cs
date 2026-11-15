using RecallCommander.Contracts.Artifacts;
using RecallCommander.Domain;

namespace RecallCommander.Markdown.Writing;

/// <summary>
/// Renders a <see cref="Review"/> as a self-contained Markdown document:
/// every question and answer copied from the attempt, followed by its
/// evaluation. Empty feedback lists render as "None." so the document reads
/// completely without knowing how it was produced.
/// </summary>
public sealed class ReviewRenderer : IArtifactRenderer<Review>
{
    private const string NoAnswer = "*No answer was provided.*";
    private const string EmptyList = "None.";

    public string Slug => "review";

    public string DirectoryName => "Reviews";

    public string Render(Review review, string artifactId)
    {
        MarkdownArtifactBuilder builder = new MarkdownArtifactBuilder()
            .WithFrontmatter(new ReviewFrontmatter(
                Slug,
                artifactId,
                review.AttemptId,
                review.Title,
                review.CreatedAtUtc,
                review.Evaluator,
                review.QuestionReviews.Count))
            .AppendHeading(1, review.Title)
            .AppendHeading(2, "Overall Summary")
            .AppendMarkdown(review.OverallSummary);

        for (int index = 0; index < review.QuestionReviews.Count; index++)
        {
            AppendQuestionReview(builder, review.QuestionReviews[index], index + 1);
        }

        return builder.Build();
    }

    private static void AppendQuestionReview(MarkdownArtifactBuilder builder, QuestionReview question, int number)
    {
        ReviewEvaluation evaluation = question.Evaluation;

        builder
            .AppendThematicBreak()
            .AppendHeading(1, $"Question {number}")
            .AppendHeading(2, "Question")
            .AppendMarkdown(question.Prompt)
            .AppendHeading(2, "Answer")
            .AppendMarkdown(question.Answer.Length > 0 ? question.Answer : NoAnswer)
            .AppendHeading(2, "Evaluation")
            .AppendMarkdown($"Score: {evaluation.Score}/{ReviewEvaluation.MaxScore}")
            .AppendMarkdown($"Understanding: {evaluation.Understanding}")
            .AppendMarkdown(evaluation.Summary)
            .AppendHeading(3, "Strengths")
            .AppendMarkdown(BulletList(evaluation.Strengths))
            .AppendHeading(3, "Missing Information")
            .AppendMarkdown(BulletList(evaluation.MissingInformation))
            .AppendHeading(3, "Incorrect Statements")
            .AppendMarkdown(BulletList(evaluation.IncorrectStatements))
            .AppendHeading(3, "Suggestions")
            .AppendMarkdown(BulletList(evaluation.Suggestions));
    }

    private static string BulletList(IReadOnlyList<string> items) =>
        items.Count == 0 ? EmptyList : string.Join('\n', items.Select(item => $"- {item}"));

    private sealed record ReviewFrontmatter(
        string Type,
        string Id,
        string? Attempt,
        string Title,
        DateTimeOffset Created,
        string? Evaluator,
        int QuestionCount);
}
