using System.ComponentModel;
using RecallCommander.Application.Sources;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RecallCommander.Cli.Commands;

public sealed class SourceAddCommand(IAnsiConsole console, QuestionSourceService sources)
    : AsyncCommand<SourceAddCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<path>")]
        [Description("Directory to register as a question source.")]
        public required string Path { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AddSourceResult result = await sources.AddAsync(settings.Path);

        switch (result.Status)
        {
            case AddSourceStatus.Added:
                console.MarkupLineInterpolated($"[green]Registered question source:[/] {result.DirectoryPath}");
                return 0;

            case AddSourceStatus.AlreadyRegistered:
                console.MarkupLineInterpolated($"Question source is already registered: {result.DirectoryPath}");
                return 0;

            default:
                console.MarkupLineInterpolated($"[red]Directory not found:[/] {result.DirectoryPath}");
                return 1;
        }
    }
}
