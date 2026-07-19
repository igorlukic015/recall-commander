namespace RecallCommander.Domain;

/// <summary>
/// The evaluation of an attempt: every question and answer as the user wrote
/// them, each with its evaluation, plus an overall summary. Like every
/// artifact, a review is a self-contained snapshot — it references the
/// attempt only by artifact id and copies all content it needs.
/// </summary>
public sealed class Review
{
    public string Title { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>
    /// The artifact id the reviewed attempt carried in its frontmatter
    /// (e.g. "assessment-2026-07-19-001"). Null when the attempt document
    /// carried no identity.
    /// </summary>
    public string? AttemptId { get; }

    /// <summary>
    /// Identity of the evaluator that produced the evaluations (e.g. a model
    /// name), when available. Null when unknown.
    /// </summary>
    public string? Evaluator { get; }

    public string OverallSummary { get; }

    public IReadOnlyList<QuestionReview> QuestionReviews { get; }

    public Review(
        string title,
        DateTimeOffset createdAtUtc,
        string overallSummary,
        IEnumerable<QuestionReview> questionReviews,
        string? attemptId = null,
        string? evaluator = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("A review title cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(overallSummary))
        {
            throw new DomainException("A review overall summary cannot be empty.");
        }

        List<QuestionReview> reviewList = questionReviews?.ToList() ?? throw new DomainException("A review requires question reviews.");
        if (reviewList.Count == 0)
        {
            throw new DomainException("A review requires at least one question review.");
        }

        Title = title.Trim();
        CreatedAtUtc = createdAtUtc;
        AttemptId = string.IsNullOrWhiteSpace(attemptId) ? null : attemptId.Trim();
        Evaluator = string.IsNullOrWhiteSpace(evaluator) ? null : evaluator.Trim();
        OverallSummary = overallSummary.Trim();
        QuestionReviews = reviewList;
    }
}

/// <summary>
/// The evaluation of one question inside a review: the prompt and answer
/// copied from the attempt, plus the evaluation. Content is copied, never
/// referenced — the review stays readable if the attempt disappears.
/// </summary>
public sealed class QuestionReview
{
    public string Prompt { get; }

    /// <summary>The user's answer, as Markdown. Empty when the question was unanswered.</summary>
    public string Answer { get; }

    public ReviewEvaluation Evaluation { get; }

    public QuestionReview(string prompt, string answer, ReviewEvaluation evaluation)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new DomainException("A question review prompt cannot be empty.");
        }

        if (answer is null)
        {
            throw new DomainException("A question review answer cannot be null. Use an empty string for an unanswered question.");
        }

        Prompt = prompt.Trim();
        Answer = answer.Trim();
        Evaluation = evaluation ?? throw new DomainException("A question review requires an evaluation.");
    }
}
