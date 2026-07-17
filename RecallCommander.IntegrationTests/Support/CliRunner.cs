using Microsoft.Extensions.DependencyInjection;
using RecallCommander.Cli;
using RecallCommander.Contracts.Artifacts;
using RecallCommander.Infrastructure.Database;
using Spectre.Console;
using Spectre.Console.Testing;

namespace RecallCommander.IntegrationTests.Support;

/// <summary>
/// Runs rc commands through the production composition root
/// (<see cref="CommandAppFactory"/>), overriding only the boundaries:
/// console output is captured, and the database plus generated artifacts
/// live inside the test workspace.
/// </summary>
public sealed class CliRunner(TestWorkspace workspace)
{
    public async Task<CliResult> RunAsync(params string[] args)
    {
        var console = new TestConsole();
        console.Profile.Width = 300;

        var app = CommandAppFactory.Create(
            services =>
            {
                services.AddSingleton<IDataPaths>(new TestDataPaths(workspace.DataDirectory));
                services.AddSingleton<IArtifactOutputPathProvider>(
                    new FixedArtifactOutputPathProvider(workspace.Root));
            },
            console);

        var exitCode = await app.RunAsync(args);
        return new CliResult(exitCode, console.Output);
    }
}

public sealed record CliResult(int ExitCode, string Output);
