namespace RecallCommander.Contracts.Questions;

/// <summary>
/// Extracts questions from the Question Blocks of a single Markdown document.
/// Invalid blocks are reported as diagnostics; parsing always continues.
/// </summary>
public interface IQuestionBlockParser
{
    QuestionParseResult Parse(string markdown);
}
