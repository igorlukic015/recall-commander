using Microsoft.Extensions.DependencyInjection;
using RecallCommander.Application;
using RecallCommander.Application.Exceptions;
using RecallCommander.Cli.Commands;
using RecallCommander.Cli.Infrastructure;
using RecallCommander.Infrastructure;
using RecallCommander.Markdown;
using Spectre.Console;
using Spectre.Console.Cli;

var services = new ServiceCollection();
services.AddSingleton(AnsiConsole.Console);
services.AddRecallCommanderApplication();
services.AddRecallCommanderMarkdown();
services.AddRecallCommanderInfrastructure();

var app = new CommandApp(new TypeRegistrar(services));

app.Configure(config =>
{
    config.SetApplicationName("rc");

    config.AddCommand<InitCommand>("init")
        .WithDescription("Initialize the Recall Commander workspace.");

    config.AddBranch("source", source =>
    {
        source.SetDescription("Manage question sources.");

        source.AddCommand<SourceAddCommand>("add")
            .WithDescription("Register a directory as a question source.");

        source.AddCommand<SourceListCommand>("list")
            .WithDescription("List registered question sources.");
    });

    config.AddCommand<ScanCommand>("scan")
        .WithDescription("Scan all question sources and report discovered questions.");

    config.AddBranch("assessment", assessment =>
    {
        assessment.SetDescription("Work with assessments.");

        assessment.AddCommand<AssessmentCreateCommand>("create")
            .WithDescription("Generate an assessment from discovered questions.");
    });

    config.SetExceptionHandler((exception, _) =>
    {
        if (exception is WorkspaceNotInitializedException or CommandRuntimeException)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]{exception.Message}[/]");
        }
        else
        {
            AnsiConsole.WriteException(exception, ExceptionFormats.ShortenEverything);
        }

        return 1;
    });
});

return app.Run(args);
