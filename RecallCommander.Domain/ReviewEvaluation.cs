namespace RecallCommander.Domain;

/// <summary>
/// The evaluation of one answered (or unanswered) question: a numeric score,
/// a descriptive understanding level and textual feedback. How the evaluation
/// was produced is deliberately not part of the model.
/// </summary>
public sealed class ReviewEvaluation
{
    public const int MinScore = 0;
    public const int MaxScore = 10;

    /// <summary>The score, from <see cref="MinScore"/> to <see cref="MaxScore"/> inclusive.</summary>
    public int Score { get; }

    public UnderstandingLevel Understanding { get; }

    public string Summary { get; }

    public IReadOnlyList<string> Strengths { get; }

    public IReadOnlyList<string> MissingInformation { get; }

    public IReadOnlyList<string> IncorrectStatements { get; }

    public IReadOnlyList<string> Suggestions { get; }

    public ReviewEvaluation(
        int score,
        UnderstandingLevel understanding,
        string summary,
        IEnumerable<string> strengths,
        IEnumerable<string> missingInformation,
        IEnumerable<string> incorrectStatements,
        IEnumerable<string> suggestions)
    {
        if (score is < MinScore or > MaxScore)
        {
            throw new DomainException($"An evaluation score must be between {MinScore} and {MaxScore}.");
        }

        if (!Enum.IsDefined(understanding))
        {
            throw new DomainException($"Unknown understanding level '{understanding}'.");
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new DomainException("An evaluation summary cannot be empty.");
        }

        Score = score;
        Understanding = understanding;
        Summary = summary.Trim();
        Strengths = Normalize(strengths, "strengths");
        MissingInformation = Normalize(missingInformation, "missing information");
        IncorrectStatements = Normalize(incorrectStatements, "incorrect statements");
        Suggestions = Normalize(suggestions, "suggestions");
    }

    /// <summary>An empty list is valid — it means the evaluation found nothing to report.</summary>
    private static IReadOnlyList<string> Normalize(IEnumerable<string> items, string listName)
    {
        List<string> list = items?.ToList() ?? throw new DomainException($"The {listName} list cannot be null.");

        if (list.Any(string.IsNullOrWhiteSpace))
        {
            throw new DomainException($"The {listName} list cannot contain empty entries.");
        }

        return list.Select(item => item.Trim()).ToList();
    }
}
