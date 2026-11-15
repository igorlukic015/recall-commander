using System.Net;
using System.Text;

namespace RecallCommander.AI.Tests.Fakes;

/// <summary>
/// Answers every HTTP request with a canned response and records the last
/// request, so provider clients can be tested without any network.
/// </summary>
public sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
        };
    }
}

/// <summary>Fails every HTTP request, simulating an unreachable provider.</summary>
public sealed class ThrowingHttpMessageHandler(string message) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        throw new HttpRequestException(message);
}
