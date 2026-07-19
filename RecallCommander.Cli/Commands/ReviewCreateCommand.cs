using System.ComponentModel;
using RecallCommander.Application.Reviews;
using RecallCommander.Contracts.Parsing;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RecallCommander.Cli.Commands;

public sealed class ReviewCreateCommand(IAnsiConsole console, CreateReviewService reviews)
    : AsyncCommand<ReviewCreateCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<file>")]
        [Description("A completed assessment (attempt) Markdown file.")]
        public required string File { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        CreateReviewResult result = await reviews.CreateAsync(settings.File);
        string attemptPath = Path.GetRelativePath(Environment.CurrentDirectory, result.AttemptFilePath);

        switch (result.Status)
        {
            case CreateReviewStatus.FileNotFound:
                console.MarkupLineInterpolated($"[red]File not found:[/] {attemptPath}");
                return 1;

            case CreateReviewStatus.InvalidAttempt:
                console.MarkupLine("[red]Attempt is not valid.[/] Fix it with [blue]rc attempt validate[/].");
                console.WriteLine();

                foreach (ParseDiagnostic diagnostic in result.Diagnostics)
                {
                    console.MarkupLineInterpolated($"[yellow]{attemptPath}:{diagnostic.LineNumber}[/]");
                    console.WriteLine(diagnostic.Message);
                    console.WriteLine();
                }

                return 1;

            default:
                string reviewPath = Path.GetRelativePath(Environment.CurrentDirectory, result.ReviewFilePath!);

                console.MarkupLine("[green]Review created.[/]");
                console.WriteLine();
                console.WriteLine($"Questions: {result.QuestionCount}");
                console.WriteLine();
                console.WriteLine("Output:");
                console.WriteLine(reviewPath);

                return 0;
        }
    }
}
