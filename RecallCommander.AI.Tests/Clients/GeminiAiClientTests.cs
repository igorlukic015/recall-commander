using System.Net;
using RecallCommander.AI.Clients;
using RecallCommander.AI.Configuration;
using RecallCommander.AI.Tests.Fakes;
using Xunit;

namespace RecallCommander.AI.Tests.Clients;

/// <summary>Gemini transport tests against a stubbed HTTP handler — no API calls, no real keys.</summary>
public sealed class GeminiAiClientTests
{
    private const string GenerateContentResponse =
        """
        {
          "candidates": [
            { "content": { "role": "model", "parts": [ { "text": "{\"score\":8}" } ] } }
          ],
          "modelVersion": "gemini-2.0-flash-001"
        }
        """;

    private static readonly AiRequest Request = new("You are a reviewer.", "Evaluate this answer.");

    private static GeminiOptions Options(string? apiKey = "test-key", string? model = "gemini-2.0-flash") => new()
    {
        Model = model,
        ApiKey = apiKey,
    };

    private static GeminiAiClient CreateClient(HttpMessageHandler handler, GeminiOptions? options = null) =>
        new(new HttpClient(handler), options ?? Options());

    [Fact]
    public async Task Posts_a_generate_content_request_and_returns_the_completion()
    {
        StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, GenerateContentResponse);

        AiResponse response = await CreateClient(handler).CompleteAsync(Request);

        Assert.Equal(
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent",
            handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(["test-key"], handler.LastRequest.Headers.GetValues("x-goog-api-key"));
        Assert.Contains("\"systemInstruction\"", handler.LastRequestBody);
        Assert.Contains("You are a reviewer.", handler.LastRequestBody);
        Assert.Contains("\"role\":\"user\"", handler.LastRequestBody);
        Assert.Contains("Evaluate this answer.", handler.LastRequestBody);

        Assert.Equal("{\"score\":8}", response.Content);
        Assert.Equal("gemini", response.Provider);
        Assert.Equal("gemini-2.0-flash-001", response.Model);
    }

    [Fact]
    public void The_name_identifies_provider_and_model()
    {
        Assert.Equal(
            "gemini/gemini-2.0-flash",
            CreateClient(new StubHttpMessageHandler(HttpStatusCode.OK, GenerateContentResponse)).Name);
    }

    [Fact]
    public void Requires_a_configured_api_key()
    {
        AiException exception = Assert.Throws<AiException>(() =>
            CreateClient(new StubHttpMessageHandler(HttpStatusCode.OK, GenerateContentResponse), Options(apiKey: null)));

        Assert.Contains("user-secrets", exception.Message);
        Assert.Contains("Ai__Gemini__ApiKey", exception.Message);
        Assert.Contains(".env", exception.Message);
    }

    [Fact]
    public void Requires_a_configured_model()
    {
        AiException exception = Assert.Throws<AiException>(() =>
            CreateClient(new StubHttpMessageHandler(HttpStatusCode.OK, GenerateContentResponse), Options(model: null)));

        Assert.Contains("Ai:Gemini:Model", exception.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task A_rejected_api_key_is_reported_clearly(HttpStatusCode statusCode)
    {
        GeminiAiClient client = CreateClient(new StubHttpMessageHandler(statusCode, """{"error":{}}"""));

        AiException exception = await Assert.ThrowsAsync<AiException>(() => client.CompleteAsync(Request));

        Assert.Contains("rejected the API key", exception.Message);
    }

    [Fact]
    public async Task A_provider_error_is_reported_with_its_status()
    {
        GeminiAiClient client = CreateClient(
            new StubHttpMessageHandler(HttpStatusCode.BadRequest, """{"error":{"message":"invalid model"}}"""));

        AiException exception = await Assert.ThrowsAsync<AiException>(() => client.CompleteAsync(Request));

        Assert.Contains("400", exception.Message);
        Assert.Contains("invalid model", exception.Message);
    }

    [Fact]
    public async Task A_response_without_candidates_is_rejected()
    {
        GeminiAiClient client = CreateClient(new StubHttpMessageHandler(HttpStatusCode.OK, "{}"));

        AiException exception = await Assert.ThrowsAsync<AiException>(() => client.CompleteAsync(Request));

        Assert.Contains("no candidate content", exception.Message);
    }
}
