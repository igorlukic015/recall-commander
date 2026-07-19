using System.Net;
using RecallCommander.AI.Clients;
using RecallCommander.AI.Configuration;
using RecallCommander.AI.Tests.Fakes;
using Xunit;

namespace RecallCommander.AI.Tests.Clients;

/// <summary>Ollama transport tests against a stubbed HTTP handler — no server involved.</summary>
public sealed class OllamaAiClientTests
{
    private const string ChatResponse =
        """
        {
          "model": "llama3.2",
          "message": { "role": "assistant", "content": "{\"score\":8}" },
          "done": true
        }
        """;

    private static readonly AiRequest Request = new("You are a reviewer.", "Evaluate this answer.");

    private static OllamaOptions Options(string? model = "llama3.2") => new()
    {
        Endpoint = "http://localhost:11434",
        Model = model,
    };

    private static OllamaAiClient CreateClient(HttpMessageHandler handler, OllamaOptions? options = null) =>
        new(new HttpClient(handler), options ?? Options());

    [Fact]
    public async Task Posts_a_chat_request_and_returns_the_completion()
    {
        StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, ChatResponse);

        AiResponse response = await CreateClient(handler).CompleteAsync(Request);

        Assert.Equal("http://localhost:11434/api/chat", handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains("\"model\":\"llama3.2\"", handler.LastRequestBody);
        Assert.Contains("\"stream\":false", handler.LastRequestBody);
        Assert.Contains("\"role\":\"system\"", handler.LastRequestBody);
        Assert.Contains("You are a reviewer.", handler.LastRequestBody);
        Assert.Contains("\"role\":\"user\"", handler.LastRequestBody);
        Assert.Contains("Evaluate this answer.", handler.LastRequestBody);

        Assert.Equal("{\"score\":8}", response.Content);
        Assert.Equal("ollama", response.Provider);
        Assert.Equal("llama3.2", response.Model);
    }

    [Fact]
    public void The_name_identifies_provider_and_model()
    {
        Assert.Equal(
            "ollama/llama3.2",
            CreateClient(new StubHttpMessageHandler(HttpStatusCode.OK, ChatResponse)).Name);
    }

    [Fact]
    public void Requires_a_configured_model()
    {
        AiException exception = Assert.Throws<AiException>(() =>
            CreateClient(new StubHttpMessageHandler(HttpStatusCode.OK, ChatResponse), Options(model: null)));

        Assert.Contains("Ai:Ollama:Model", exception.Message);
    }

    [Fact]
    public async Task A_provider_error_is_reported_with_its_status()
    {
        OllamaAiClient client = CreateClient(
            new StubHttpMessageHandler(HttpStatusCode.InternalServerError, """{"error":"model not found"}"""));

        AiException exception = await Assert.ThrowsAsync<AiException>(() => client.CompleteAsync(Request));

        Assert.Contains("500", exception.Message);
        Assert.Contains("model not found", exception.Message);
    }

    [Fact]
    public async Task An_unreachable_server_is_reported_clearly()
    {
        OllamaAiClient client = CreateClient(new ThrowingHttpMessageHandler("Connection refused"));

        AiException exception = await Assert.ThrowsAsync<AiException>(() => client.CompleteAsync(Request));

        Assert.Contains("Cannot reach Ollama", exception.Message);
        Assert.Contains("ollama serve", exception.Message);
    }

    [Fact]
    public async Task A_response_without_content_is_rejected()
    {
        OllamaAiClient client = CreateClient(
            new StubHttpMessageHandler(HttpStatusCode.OK, """{"model":"llama3.2","done":true}"""));

        AiException exception = await Assert.ThrowsAsync<AiException>(() => client.CompleteAsync(Request));

        Assert.Contains("no message content", exception.Message);
    }
}
