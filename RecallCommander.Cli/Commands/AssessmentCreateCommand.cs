using System.ComponentModel;
using RecallCommander.Application.Assessments;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RecallCommander.Cli.Commands;

public sealed class AssessmentCreateCommand(IAnsiConsole console, CreateAssessmentService assessments)
    : AsyncCommand<AssessmentCreateCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--count <COUNT>")]
        [Description("Number of questions to include. Defaults to 10.")]
        public int? Count { get; init; }

        public override ValidationResult Validate() =>
            Count is null or > 0
                ? ValidationResult.Success()
                : ValidationResult.Error("--count must be greater than zero.");
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        CreateAssessmentResult result = await assessments.CreateAsync(settings.Count);

        if (result.Status == CreateAssessmentStatus.NoQuestionsFound)
        {
            console.MarkupLine("[red]No questions found.[/] Check your sources with [blue]rc scan[/].");
            return 1;
        }

        string displayPath = Path.GetRelativePath(Environment.CurrentDirectory, result.FilePath!);

        console.MarkupLine("[green]Assessment created.[/]");
        console.WriteLine();
        console.WriteLine($"Questions: {result.QuestionCount}");
        console.WriteLine();
        console.WriteLine("Output:");
        console.WriteLine(displayPath);

        return 0;
    }
}
