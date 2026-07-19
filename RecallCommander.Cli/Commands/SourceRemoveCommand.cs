using System.ComponentModel;
using RecallCommander.Application.Sources;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RecallCommander.Cli.Commands;

public sealed class SourceRemoveCommand(IAnsiConsole console, QuestionSourceService sources)
    : AsyncCommand<SourceRemoveCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<path>")]
        [Description("Registered question source directory to remove. The directory itself is never touched.")]
        public required string Path { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        RemoveSourceResult result = await sources.RemoveAsync(settings.Path);

        if (result.Status == RemoveSourceStatus.Removed)
        {
            console.MarkupLineInterpolated($"[green]Removed question source:[/] {result.DirectoryPath}");
            return 0;
        }

        console.MarkupLineInterpolated($"[red]Question source is not registered:[/] {result.DirectoryPath}");
        console.MarkupLine("Use [blue]rc source list[/] to see the registered sources.");
        return 1;
    }
}
