using RecallCommander.Contracts.Parsing;
using RecallCommander.Contracts.Questions;
using RecallCommander.Domain;

namespace RecallCommander.Application.Tests.Fakes;

/// <summary>
/// Interprets each input line as a directive: "question" produces one discovered
/// question, "warning:<message>" produces one diagnostic.
/// </summary>
public sealed class FakeQuestionBlockParser : IQuestionBlockParser
{
    public QuestionParseResult Parse(string markdown)
    {
        List<DiscoveredQuestion> questions = new List<DiscoveredQuestion>();
        List<ParseDiagnostic> diagnostics = new List<ParseDiagnostic>();

        string[] lines = markdown.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index].Trim();
            if (line == "question")
            {
                questions.Add(new DiscoveredQuestion(
                    new Question(QuestionType.Recall, "What?"),
                    index + 1));
            }
            else if (line.StartsWith("warning:", StringComparison.Ordinal))
            {
                diagnostics.Add(new ParseDiagnostic(index + 1, line["warning:".Length..]));
            }
        }

        return new QuestionParseResult(questions, diagnostics);
    }
}
