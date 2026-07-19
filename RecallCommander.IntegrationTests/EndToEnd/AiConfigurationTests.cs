using Microsoft.Extensions.Configuration;
using RecallCommander.IntegrationTests.Support;
using Xunit;

namespace RecallCommander.IntegrationTests.EndToEnd;

/// <summary>
/// AI configuration behavior through the CLI entry point. AI settings must
/// never break commands that don't use AI, and configuration mistakes must
/// surface as clean messages, not stack traces. Nothing here performs a
/// network call.
/// </summary>
public sealed class AiConfigurationTests : IDisposable
{
    private readonly TestWorkspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    private CliRunner CreateCli(params (string Key, string Value)[] settings) => new(
        _workspace,
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(
                setting => setting.Key,
                string? (setting) => setting.Value))
            .Build());

    [Fact]
    public async Task An_unknown_provider_fails_with_a_clean_message()
    {
        CliRunner cli = CreateCli(("Ai:Provider", "chatgpt"));

        CliResult result = await cli.RunAsync("review", "create", "nope.md");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Unknown AI provider 'chatgpt'", result.Output);
    }

    [Fact]
    public async Task A_missing_gemini_api_key_fails_with_a_clean_message()
    {
        CliRunner cli = CreateCli(
            ("Ai:Provider", "gemini"),
            ("Ai:Gemini:Model", "gemini-2.0-flash"));

        CliResult result = await cli.RunAsync("review", "create", "nope.md");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Gemini API key is not configured", result.Output);
    }

    [Fact]
    public async Task Ai_configuration_does_not_affect_commands_without_ai()
    {
        CliRunner cli = CreateCli(("Ai:Provider", "chatgpt"));

        CliResult result = await cli.RunAsync("init");

        Assert.Equal(0, result.ExitCode);
    }
}
