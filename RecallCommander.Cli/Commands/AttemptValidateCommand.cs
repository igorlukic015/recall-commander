using System.ComponentModel;
using RecallCommander.Application.Attempts;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RecallCommander.Cli.Commands;

public sealed class AttemptValidateCommand(IAnsiConsole console, ValidateAttemptService attempts)
    : Command<AttemptValidateCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<file>")]
        [Description("A completed assessment Markdown file.")]
        public required string File { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var result = attempts.Validate(settings.File);
        var displayPath = Path.GetRelativePath(Environment.CurrentDirectory, result.FilePath);

        switch (result.Status)
        {
            case ValidateAttemptStatus.FileNotFound:
                console.MarkupLineInterpolated($"[red]File not found:[/] {displayPath}");
                return 1;

            case ValidateAttemptStatus.Invalid:
                console.MarkupLine("[red]Attempt is not valid.[/]");
                console.WriteLine();

                foreach (var diagnostic in result.Diagnostics)
                {
                    console.MarkupLineInterpolated($"[yellow]{displayPath}:{diagnostic.LineNumber}[/]");
                    console.WriteLine(diagnostic.Message);
                    console.WriteLine();
                }

                return 1;

            default:
                var attempt = result.Attempt!;

                console.MarkupLine("[green]Attempt is valid.[/]");
                console.WriteLine();
                console.WriteLine($"Title: {attempt.Title}");
                console.WriteLine($"Questions: {attempt.Questions.Count}");
                console.WriteLine($"Answered: {attempt.Questions.Count(question => question.IsAnswered)}");

                return 0;
        }
    }
}
