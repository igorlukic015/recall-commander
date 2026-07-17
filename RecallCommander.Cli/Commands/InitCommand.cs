using RecallCommander.Contracts.Workspace;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RecallCommander.Cli.Commands;

public sealed class InitCommand(IAnsiConsole console, IWorkspaceInitializer initializer) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var result = await initializer.InitializeAsync();

        console.MarkupLine(result.Created
            ? "[green]Initialized Recall Commander workspace.[/]"
            : "Workspace is already initialized.");
        console.MarkupLineInterpolated($"[dim]Database: {result.DatabasePath}[/]");

        return 0;
    }
}
