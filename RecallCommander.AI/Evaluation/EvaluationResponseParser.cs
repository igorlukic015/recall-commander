using System.Text.Json;
using RecallCommander.Domain;

namespace RecallCommander.AI.Evaluation;

/// <summary>
/// Parses the model's JSON evaluation into a <see cref="ReviewEvaluation"/>.
/// Tolerates the noise models add around JSON (code fences, surrounding
/// prose) but rejects anything that does not yield a valid evaluation, so a
/// confused model can never produce a silently wrong review.
/// </summary>
public sealed class EvaluationResponseParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public ReviewEvaluation Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new AiException("The AI provider returned an empty response.");
        }

        EvaluationDocument document = Deserialize(ExtractJson(content));

        if (document.Score is null)
        {
            throw new AiException("The AI evaluation is missing the required 'score' field.");
        }

        if (string.IsNullOrWhiteSpace(document.Level))
        {
            throw new AiException("The AI evaluation is missing the required 'level' field.");
        }

        if (!Enum.TryParse(document.Level.Trim(), ignoreCase: true, out UnderstandingLevel level))
        {
            throw new AiException(
                $"The AI evaluation has an unknown understanding level '{document.Level.Trim()}'. " +
                $"Expected one of: {string.Join(", ", Enum.GetNames<UnderstandingLevel>())}.");
        }

        if (string.IsNullOrWhiteSpace(document.Summary))
        {
            throw new AiException("The AI evaluation is missing the required 'summary' field.");
        }

        try
        {
            return new ReviewEvaluation(
                document.Score.Value,
                level,
                document.Summary,
                NonBlank(document.Strengths),
                NonBlank(document.MissingInformation),
                NonBlank(document.IncorrectStatements),
                NonBlank(document.Suggestions));
        }
        catch (DomainException exception)
        {
            throw new AiException($"The AI evaluation is invalid: {exception.Message}", exception);
        }
    }

    private static EvaluationDocument Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<EvaluationDocument>(json, SerializerOptions)
                ?? throw new AiException("The AI response contained no evaluation object.");
        }
        catch (JsonException exception)
        {
            throw new AiException("The AI response is not valid JSON.", exception);
        }
    }

    /// <summary>
    /// Cuts the JSON object out of the raw response. Models are instructed to
    /// answer with bare JSON but often wrap it in code fences or prose anyway.
    /// </summary>
    private static string ExtractJson(string content)
    {
        int start = content.IndexOf('{');
        int end = content.LastIndexOf('}');

        if (start < 0 || end <= start)
        {
            throw new AiException("The AI response contains no JSON object.");
        }

        return content.Substring(start, end - start + 1);
    }

    private static List<string> NonBlank(List<string?>? items) =>
        items is null
            ? []
            : items.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item!).ToList();

    private sealed class EvaluationDocument
    {
        public int? Score { get; set; }

        public string? Level { get; set; }

        public string? Summary { get; set; }

        public List<string?>? Strengths { get; set; }

        public List<string?>? MissingInformation { get; set; }

        public List<string?>? IncorrectStatements { get; set; }

        public List<string?>? Suggestions { get; set; }
    }
}
