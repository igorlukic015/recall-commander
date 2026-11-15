using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RecallCommander.AI.Configuration;

namespace RecallCommander.AI.Clients;

/// <summary>
/// Talks to the Gemini REST API (generateContent). The API key comes from
/// <see cref="GeminiOptions"/> — supplied through user secrets or environment
/// variables, never source — and is sent as the x-goog-api-key header.
/// </summary>
public sealed class GeminiAiClient : IAiClient
{
    private const string ProviderName = "gemini";
    private const string ApiKeyHeader = "x-goog-api-key";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly string _model;

    public GeminiAiClient(HttpClient http, GeminiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            throw new AiException("The Gemini endpoint is not configured. Set Ai:Gemini:Endpoint.");
        }

        if (string.IsNullOrWhiteSpace(options.Model))
        {
            throw new AiException("The Gemini model is not configured. Set Ai:Gemini:Model, e.g. 'gemini-2.0-flash'.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new AiException(
                "The Gemini API key is not configured. Set it with "
                + "'dotnet user-secrets set Ai:Gemini:ApiKey <key>' or add "
                + "Ai__Gemini__ApiKey to the gitignored .env file in your workspace.");
        }

        _http = http;
        _http.BaseAddress = new Uri(options.Endpoint.TrimEnd('/') + "/");
        _http.DefaultRequestHeaders.Add(ApiKeyHeader, options.ApiKey.Trim());
        _model = options.Model.Trim();
    }

    public string Name => $"{ProviderName}/{_model}";

    public async Task<AiResponse> CompleteAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        GenerateContentRequest payload = new GenerateContentRequest(
            new Content(null, [new Part(request.SystemPrompt)]),
            [new Content("user", [new Part(request.UserPrompt)])]);

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync(
                $"models/{_model}:generateContent", payload, SerializerOptions, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new AiException($"Cannot reach Gemini at {_http.BaseAddress}: {exception.Message}", exception);
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new AiException("Gemini rejected the API key. Check Ai:Gemini:ApiKey.");
        }

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new AiException($"Gemini returned {(int)response.StatusCode}: {Truncate(error)}");
        }

        GenerateContentResponse? completion = await response.Content.ReadFromJsonAsync<GenerateContentResponse>(
            SerializerOptions, cancellationToken);

        string? text = completion?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        if (text is null)
        {
            throw new AiException("Gemini returned no candidate content.");
        }

        return new AiResponse(text, ProviderName, completion?.ModelVersion ?? _model);
    }

    private static string Truncate(string text) =>
        text.Length <= 500 ? text : text[..500];

    private sealed record GenerateContentRequest(Content SystemInstruction, IReadOnlyList<Content> Contents);

    private sealed record Content(string? Role, IReadOnlyList<Part>? Parts);

    private sealed record Part(string? Text);

    private sealed record GenerateContentResponse(IReadOnlyList<Candidate>? Candidates, string? ModelVersion);

    private sealed record Candidate(Content? Content);
}
