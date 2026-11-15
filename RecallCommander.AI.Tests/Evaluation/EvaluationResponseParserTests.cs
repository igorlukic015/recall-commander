using RecallCommander.AI.Evaluation;
using RecallCommander.Domain;
using Xunit;

namespace RecallCommander.AI.Tests.Evaluation;

public sealed class EvaluationResponseParserTests
{
    private readonly EvaluationResponseParser _parser = new();

    [Fact]
    public void Parses_a_minimal_evaluation()
    {
        ReviewEvaluation evaluation = _parser.Parse(
            """{"score":8,"level":"Strong","summary":"Good understanding"}""");

        Assert.Equal(8, evaluation.Score);
        Assert.Equal(UnderstandingLevel.Strong, evaluation.Understanding);
        Assert.Equal("Good understanding", evaluation.Summary);
        Assert.Empty(evaluation.Strengths);
        Assert.Empty(evaluation.MissingInformation);
        Assert.Empty(evaluation.IncorrectStatements);
        Assert.Empty(evaluation.Suggestions);
    }

    [Fact]
    public void Parses_a_complete_evaluation()
    {
        ReviewEvaluation evaluation = _parser.Parse(
            """
            {
              "score": 6,
              "level": "Good",
              "summary": "Mostly right.",
              "strengths": ["Clear structure."],
              "missing_information": ["Generations.", "The large object heap."],
              "incorrect_statements": ["The GC does not run on a timer."],
              "suggestions": ["Mention generations."]
            }
            """);

        Assert.Equal(6, evaluation.Score);
        Assert.Equal(UnderstandingLevel.Good, evaluation.Understanding);
        Assert.Equal(["Clear structure."], evaluation.Strengths);
        Assert.Equal(["Generations.", "The large object heap."], evaluation.MissingInformation);
        Assert.Equal(["The GC does not run on a timer."], evaluation.IncorrectStatements);
        Assert.Equal(["Mention generations."], evaluation.Suggestions);
    }

    [Fact]
    public void Parses_json_wrapped_in_code_fences()
    {
        ReviewEvaluation evaluation = _parser.Parse(
            "```json\n{\"score\":8,\"level\":\"Strong\",\"summary\":\"Good understanding\"}\n```");

        Assert.Equal(8, evaluation.Score);
    }

    [Fact]
    public void Parses_json_surrounded_by_prose()
    {
        ReviewEvaluation evaluation = _parser.Parse(
            "Here is my evaluation:\n{\"score\":5,\"level\":\"Partial\",\"summary\":\"Half way.\"}\nHope this helps!");

        Assert.Equal(5, evaluation.Score);
        Assert.Equal(UnderstandingLevel.Partial, evaluation.Understanding);
    }

    [Fact]
    public void Parses_the_level_case_insensitively()
    {
        ReviewEvaluation evaluation = _parser.Parse(
            """{"score":8,"level":"strong","summary":"Good understanding"}""");

        Assert.Equal(UnderstandingLevel.Strong, evaluation.Understanding);
    }

    [Fact]
    public void Filters_blank_feedback_entries()
    {
        ReviewEvaluation evaluation = _parser.Parse(
            """{"score":8,"level":"Strong","summary":"Fine.","strengths":["Clear.","","   "]}""");

        Assert.Equal(["Clear."], evaluation.Strengths);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_an_empty_response(string content)
    {
        AiException exception = Assert.Throws<AiException>(() => _parser.Parse(content));

        Assert.Contains("empty response", exception.Message);
    }

    [Fact]
    public void Rejects_a_response_without_json()
    {
        AiException exception = Assert.Throws<AiException>(() =>
            _parser.Parse("Sorry, I cannot evaluate this answer."));

        Assert.Contains("no JSON object", exception.Message);
    }

    [Fact]
    public void Rejects_invalid_json()
    {
        AiException exception = Assert.Throws<AiException>(() =>
            _parser.Parse("{score: not-valid-json}"));

        Assert.Contains("not valid JSON", exception.Message);
    }

    [Fact]
    public void Rejects_a_missing_score()
    {
        AiException exception = Assert.Throws<AiException>(() =>
            _parser.Parse("""{"level":"Strong","summary":"Fine."}"""));

        Assert.Contains("'score'", exception.Message);
    }

    [Fact]
    public void Rejects_a_missing_level()
    {
        AiException exception = Assert.Throws<AiException>(() =>
            _parser.Parse("""{"score":8,"summary":"Fine."}"""));

        Assert.Contains("'level'", exception.Message);
    }

    [Fact]
    public void Rejects_a_missing_summary()
    {
        AiException exception = Assert.Throws<AiException>(() =>
            _parser.Parse("""{"score":8,"level":"Strong"}"""));

        Assert.Contains("'summary'", exception.Message);
    }

    [Fact]
    public void Rejects_an_unknown_level()
    {
        AiException exception = Assert.Throws<AiException>(() =>
            _parser.Parse("""{"score":8,"level":"Superb","summary":"Fine."}"""));

        Assert.Contains("Superb", exception.Message);
        Assert.Contains("Excellent", exception.Message);
    }

    [Fact]
    public void Rejects_a_score_outside_the_valid_range()
    {
        AiException exception = Assert.Throws<AiException>(() =>
            _parser.Parse("""{"score":11,"level":"Strong","summary":"Fine."}"""));

        Assert.Contains("between 0 and 10", exception.Message);
    }
}
