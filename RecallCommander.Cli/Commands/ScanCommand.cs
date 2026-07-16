using RecallCommander.Application.Scanning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RecallCommander.Cli.Commands;

public sealed class ScanCommand(IAnsiConsole console, ScanService scanner) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var report = await scanner.ScanAsync();

        if (report.SourceCount == 0)
        {
            console.MarkupLine("No question sources registered. Use [blue]rc source add <path>[/] to add one.");
            return 0;
        }

        console.WriteLine("Scanning...");
        console.WriteLine();

        foreach (var file in report.Files.Where(file => file.Questions.Count > 0))
        {
            console.MarkupLineInterpolated($"[bold]{file.DisplayPath}[/]");
            var count = file.Questions.Count;
            console.WriteLine($"  Found {count} {(count == 1 ? "question" : "questions")}");
            console.WriteLine();
        }

        if (report.Warnings.Count > 0)
        {
            console.MarkupLine("[yellow]Warnings:[/]");
            console.WriteLine();

            foreach (var warning in report.Warnings)
            {
                console.MarkupLineInterpolated($"[yellow]{warning.Location}[/]");
                console.WriteLine(warning.Message);
                console.WriteLine();
            }
        }

        console.WriteLine("Scan completed.");
        console.WriteLine();
        console.MarkupLineInterpolated($"Questions discovered: [bold]{report.TotalQuestions}[/]");

        return 0;
    }
}
