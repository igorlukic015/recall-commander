
namespace RecallCommander.Domain;

/// <summary>
/// A snapshot of questions selected at one point in time. Question text is
/// copied in, so an assessment stays valid even if the original Question
/// Sources change or disappear.
/// </summary>
public sealed class Assessment
{
    public string Title { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyList<AssessmentQuestion> Questions { get; }

    public Assessment(string title, DateTimeOffset createdAtUtc, IEnumerable<AssessmentQuestion> questions)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("An assessment title cannot be empty.");
        }

        List<AssessmentQuestion> questionList = questions?.ToList() ?? throw new DomainException("An assessment requires questions.");
        if (questionList.Count == 0)
        {
            throw new DomainException("An assessment requires at least one question.");
        }

        Title = title.Trim();
        CreatedAtUtc = createdAtUtc;
        Questions = questionList;
    }
}

/// <summary>
/// A question as captured inside an assessment: only the prompt text, copied
/// from the discovered question. Types, concepts and reference answers are
/// deliberately not part of the snapshot the user sees.
/// </summary>
public sealed class AssessmentQuestion
{
    public string Prompt { get; }

    public AssessmentQuestion(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new DomainException("An assessment question prompt cannot be empty.");
        }

        Prompt = prompt.Trim();
    }

    public static AssessmentQuestion FromQuestion(Question question) => new(question.Prompt);
}
