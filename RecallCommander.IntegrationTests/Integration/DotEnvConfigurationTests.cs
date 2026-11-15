using Microsoft.Extensions.Configuration;
using RecallCommander.Infrastructure.Configuration;
using RecallCommander.IntegrationTests.Support;
using Xunit;

namespace RecallCommander.IntegrationTests.Integration;

/// <summary>
/// The gitignored .env file as a configuration source: parsing rules and its
/// place at the top of the precedence order.
/// </summary>
public sealed class DotEnvConfigurationTests : IDisposable
{
    private readonly TestWorkspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    private IConfiguration LoadDotEnv(string content)
    {
        string path = _workspace.WriteFile(_workspace.Root, ".env", content);
        return new ConfigurationBuilder().AddDotEnvFile(path).Build();
    }

    [Fact]
    public void Maps_double_underscores_to_configuration_sections()
    {
        IConfiguration configuration = LoadDotEnv(
            """
            Ai__Provider=ollama
            Ai__Ollama__Model=llama3.2
            """);

        Assert.Equal("ollama", configuration["Ai:Provider"]);
        Assert.Equal("llama3.2", configuration["Ai:Ollama:Model"]);
    }

    [Fact]
    public void Skips_comments_blank_lines_and_malformed_lines()
    {
        IConfiguration configuration = LoadDotEnv(
            """
            # AI settings for this workspace

            Ai__Provider=ollama
            this line has no separator
            =no key
            """);

        Assert.Equal("ollama", configuration["Ai:Provider"]);
        Assert.DoesNotContain(
            configuration.AsEnumerable(),
            pair => pair.Key.Contains("separator", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Strips_quotes_and_the_export_prefix()
    {
        IConfiguration configuration = LoadDotEnv(
            """
            Ai__Gemini__ApiKey="secret-key"
            export Ai__Gemini__Model='gemini-2.0-flash'
            """);

        Assert.Equal("secret-key", configuration["Ai:Gemini:ApiKey"]);
        Assert.Equal("gemini-2.0-flash", configuration["Ai:Gemini:Model"]);
    }

    [Fact]
    public void Preserves_values_containing_equals_signs()
    {
        IConfiguration configuration = LoadDotEnv("Ai__Gemini__ApiKey=abc=def==");

        Assert.Equal("abc=def==", configuration["Ai:Gemini:ApiKey"]);
    }

    [Fact]
    public void A_missing_file_is_not_an_error()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddDotEnvFile(Path.Combine(_workspace.Root, "nope.env"))
            .Build();

        Assert.Empty(configuration.AsEnumerable());
    }

    [Fact]
    public void Dot_env_values_override_earlier_sources()
    {
        string path = _workspace.WriteFile(_workspace.Root, ".env", "Ai__Provider=ollama");

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Ai:Provider"] = "fake" })
            .AddDotEnvFile(path)
            .Build();

        Assert.Equal("ollama", configuration["Ai:Provider"]);
    }

    [Fact]
    public async Task A_dot_env_file_drives_the_cli_configuration()
    {
        string path = _workspace.WriteFile(_workspace.Root, ".env", "Ai__Provider=chatgpt");
        CliRunner cli = new CliRunner(
            _workspace,
            new ConfigurationBuilder().AddDotEnvFile(path).Build());

        CliResult result = await cli.RunAsync("review", "create", "nope.md");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Unknown AI provider 'chatgpt'", result.Output);
    }
}
