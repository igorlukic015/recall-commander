using RecallCommander.Application.Sources;
using RecallCommander.Domain;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RecallCommander.Cli.Commands;

public sealed class SourceListCommand(IAnsiConsole console, QuestionSourceService sources) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        IReadOnlyList<QuestionSource> all = await sources.ListAsync();

        if (all.Count == 0)
        {
            console.MarkupLine("No question sources registered. Use [blue]rc source add <path>[/] to add one.");
            return 0;
        }

        Table table = new Table()
            .AddColumn("Id")
            .AddColumn("Path")
            .AddColumn("Registered (UTC)");

        foreach (QuestionSource source in all)
        {
            table.AddRow(
                source.Id.ToString(),
                Markup.Escape(source.DirectoryPath),
                source.RegisteredAtUtc.ToString("yyyy-MM-dd HH:mm"));
        }

        console.Write(table);
        return 0;
    }
}
