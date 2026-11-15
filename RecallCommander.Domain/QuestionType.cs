namespace RecallCommander.Domain;

/// <summary>
/// The category of a question, describing what kind of knowledge it assesses.
/// </summary>
public enum QuestionType
{
    /// <summary>Can I retrieve and explain a fact or idea?</summary>
    Recall,

    /// <summary>Can I explain a concept in detail?</summary>
    Explanation,

    /// <summary>Can I connect multiple concepts and form a deeper understanding?</summary>
    Synthesis,
}
