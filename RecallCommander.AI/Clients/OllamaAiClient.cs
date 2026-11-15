using System.Net.Http.Json;
using System.Text.Json;
using RecallCommander.AI.Configuration;

namespace RecallCommander.AI.Clients;

/// <summary>
/// Talks to a local Ollama server through its /api/chat endpoint. Needs no
/// authentication; endpoint and model come from <see cref="OllamaOptions"/>.
/// </summary>
public sealed class OllamaAiClient : IAiClient
{
    private const string ProviderName = "ollama";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly string _model;

    public OllamaAiClient(HttpClient http, OllamaOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            throw new AiException("The Ollama endpoint is not configured. Set Ai:Ollama:Endpoint.");
        }

        if (string.IsNullOrWhiteSpace(options.Model))
        {
            throw new AiException("The Ollama model is not configured. Set Ai:Ollama:Model, e.g. 'llama3.2'.");
        }

        _http = http;
        _http.BaseAddress = new Uri(options.Endpoint.TrimEnd('/') + "/");
        _model = options.Model.Trim();
    }

    public string Name => $"{ProviderName}/{_model}";

    public async Task<AiResponse> CompleteAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ChatRequest payload = new ChatRequest(
            _model,
            [
                new ChatMessage("system", request.SystemPrompt),
                new ChatMessage("user", request.UserPrompt),
            ],
            Stream: false);

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("api/chat", payload, SerializerOptions, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new AiException(
                $"Cannot reach Ollama at {_http.BaseAddress}: {exception.Message} Is 'ollama serve' running?",
                exception);
        }

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new AiException($"Ollama returned {(int)response.StatusCode}: {Truncate(error)}");
        }

        ChatResponse? completion = await response.Content.ReadFromJsonAsync<ChatResponse>(
            SerializerOptions, cancellationToken);

        if (completion?.Message?.Content is null)
        {
            throw new AiException("Ollama returned no message content.");
        }

        return new AiResponse(completion.Message.Content, ProviderName, completion.Model ?? _model);
    }

    private static string Truncate(string text) =>
        text.Length <= 500 ? text : text[..500];

    private sealed record ChatRequest(string Model, IReadOnlyList<ChatMessage> Messages, bool Stream);

    private sealed record ChatMessage(string Role, string Content);

    private sealed record ChatResponse(string? Model, ChatMessage? Message);
}
