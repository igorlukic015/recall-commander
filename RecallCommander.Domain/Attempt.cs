namespace RecallCommander.Domain;

/// <summary>
/// A completed assessment: the questions as the user saw them together with
/// the answers the user wrote. Attempts are authored by the user (Save As on
/// an assessment file), never generated. Like an assessment, an attempt is a
/// self-contained snapshot; it references no questions or question sources.
/// </summary>
public sealed class Attempt
{
    public string Title { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyList<AttemptQuestion> Questions { get; }

    public Attempt(string title, DateTimeOffset createdAtUtc, IEnumerable<AttemptQuestion> questions)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("An attempt title cannot be empty.");
        }

        var questionList = questions?.ToList() ?? throw new DomainException("An attempt requires questions.");
        if (questionList.Count == 0)
        {
            throw new DomainException("An attempt requires at least one question.");
        }

        Title = title.Trim();
        CreatedAtUtc = createdAtUtc;
        Questions = questionList;
    }
}

/// <summary>
/// A question inside an attempt: the prompt as it appeared in the assessment
/// and the user's answer. An empty answer is valid — it means the user left
/// the question unanswered.
/// </summary>
public sealed class AttemptQuestion
{
    public string Prompt { get; }

    /// <summary>The user's answer, as Markdown. Empty when unanswered.</summary>
    public string Answer { get; }

    public bool IsAnswered => Answer.Length > 0;

    public AttemptQuestion(string prompt, string answer)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new DomainException("An attempt question prompt cannot be empty.");
        }

        if (answer is null)
        {
            throw new DomainException("An attempt answer cannot be null. Use an empty string for an unanswered question.");
        }

        Prompt = prompt.Trim();
        Answer = answer.Trim();
    }
}
