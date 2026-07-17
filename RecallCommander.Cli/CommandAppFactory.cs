using Microsoft.Extensions.DependencyInjection;
using RecallCommander.Application;
using RecallCommander.Cli.Commands;
using RecallCommander.Cli.Infrastructure;
using RecallCommander.Contracts.Exceptions;
using RecallCommander.Infrastructure;
using RecallCommander.Markdown;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RecallCommander.Cli;

/// <summary>
/// Composition root for the rc command line application. Tests use
/// <paramref name="configureServices"/> to override boundary services (data
/// paths, artifact output location) and <paramref name="console"/> to capture
/// output — the command pipeline itself stays exactly what production runs.
/// </summary>
public static class CommandAppFactory
{
    public static CommandApp Create(
        Action<IServiceCollection>? configureServices = null,
        IAnsiConsole? console = null)
    {
        ServiceCollection services = new ServiceCollection();
        services.AddRecallCommanderApplication();
        services.AddRecallCommanderMarkdown();
        services.AddRecallCommanderInfrastructure();
        configureServices?.Invoke(services);

        CommandApp app = new CommandApp(new TypeRegistrar(services));

        app.Configure(config =>
        {
            config.SetApplicationName("rc");

            if (console is not null)
            {
                // Spectre registers this console into DI, so commands
                // injecting IAnsiConsole receive it as well.
                config.ConfigureConsole(console);
            }

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

            config.AddBranch("attempt", attempt =>
            {
                attempt.SetDescription("Work with completed assessment attempts.");

                attempt.AddCommand<AttemptValidateCommand>("validate")
                    .WithDescription("Parse a completed assessment file and report problems.");
            });

            config.SetExceptionHandler((exception, resolver) =>
            {
                IAnsiConsole console = resolver?.Resolve(typeof(IAnsiConsole)) as IAnsiConsole ?? AnsiConsole.Console;

                if (exception is WorkspaceNotInitializedException or CommandRuntimeException)
                {
                    console.MarkupLineInterpolated($"[red]{exception.Message}[/]");
                }
                else
                {
                    console.WriteException(exception, ExceptionFormats.ShortenEverything);
                }

                return 1;
            });
        });

        return app;
    }
}
