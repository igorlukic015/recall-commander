namespace RecallCommander.Domain;

/// <summary>
/// A single assessment question authored by the user in a Markdown Question Block.
/// Questions are discovered on every scan and never persisted; the Markdown source
/// remains the authority.
/// </summary>
public sealed class Question
{
    public QuestionType Type { get; }

    /// <summary>The question text, as Markdown.</summary>
    public string Prompt { get; }

    /// <summary>An optional reference answer, as Markdown.</summary>
    public string? ReferenceAnswer { get; }

    /// <summary>Concepts this question relates to. Metadata only; may be empty.</summary>
    public IReadOnlyList<string> Concepts { get; }

    public Question(
        QuestionType type,
        string prompt,
        string? referenceAnswer = null,
        IEnumerable<string>? concepts = null)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new DomainException("A question prompt cannot be empty.");
        }

        if (referenceAnswer is not null && string.IsNullOrWhiteSpace(referenceAnswer))
        {
            throw new DomainException("A reference answer cannot be empty. Omit it instead.");
        }

        Type = type;
        Prompt = prompt.Trim();
        ReferenceAnswer = referenceAnswer?.Trim();
        Concepts = NormalizeConcepts(concepts);
    }

    private static IReadOnlyList<string> NormalizeConcepts(IEnumerable<string>? concepts)
    {
        if (concepts is null)
        {
            return [];
        }

        List<string> normalized = new List<string>();
        foreach (string concept in concepts)
        {
            if (string.IsNullOrWhiteSpace(concept))
            {
                throw new DomainException("A concept cannot be empty.");
            }

            string trimmed = concept.Trim();
            if (!normalized.Contains(trimmed, StringComparer.Ordinal))
            {
                normalized.Add(trimmed);
            }
        }

        return normalized;
    }
}
