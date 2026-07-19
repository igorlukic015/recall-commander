using RecallCommander.Application.Assessments;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RecallCommander.Cli.Commands;

public sealed class AssessmentListCommand(IAnsiConsole console, AssessmentLocator assessments) : Command
{
    public override int Execute(CommandContext context)
    {
        IReadOnlyList<ArtifactFile> all = assessments.List();

        if (all.Count == 0)
        {
            console.MarkupLine("No assessments found. Use [blue]rc assessment create[/] to generate one.");
            return 0;
        }

        Table table = new Table()
            .AddColumn("Assessment (newest first)")
            .AddColumn("Path");

        foreach (ArtifactFile assessment in all)
        {
            table.AddRow(
                Markup.Escape(assessment.FileName),
                Markup.Escape(Path.GetRelativePath(Environment.CurrentDirectory, assessment.FilePath)));
        }

        console.Write(table);
        return 0;
    }
}
