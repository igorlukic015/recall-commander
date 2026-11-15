namespace RecallCommander.Domain;

/// <summary>
/// A descriptive judgement of how well an answer demonstrates understanding,
/// complementing the numeric score of a <see cref="ReviewEvaluation"/>.
/// </summary>
public enum UnderstandingLevel
{
    Poor,

    Weak,

    Partial,

    Good,

    Strong,

    Excellent,
}
