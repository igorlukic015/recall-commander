using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecallCommander.AI;
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
/// paths, artifact output location), <paramref name="console"/> to capture
/// output and <paramref name="configuration"/> to pin configuration — the
/// command pipeline itself stays exactly what production runs.
/// </summary>
public static class CommandAppFactory
{
    public static CommandApp Create(
        Action<IServiceCollection>? configureServices = null,
        IAnsiConsole? console = null,
        IConfiguration? configuration = null)
    {
        ServiceCollection services = new ServiceCollection();
        services.AddRecallCommanderApplication();
        services.AddRecallCommanderMarkdown();
        services.AddRecallCommanderInfrastructure();
        services.AddRecallCommanderAi(configuration ?? BuildConfiguration());
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

            config.AddBranch("review", review =>
            {
                review.SetDescription("Work with attempt reviews.");

                review.AddCommand<ReviewCreateCommand>("create")
                    .WithDescription("Evaluate a completed attempt and write a review artifact.");
            });

            config.SetExceptionHandler((exception, resolver) =>
            {
                IAnsiConsole console = resolver?.Resolve(typeof(IAnsiConsole)) as IAnsiConsole ?? AnsiConsole.Console;

                // Spectre wraps failures during command resolution (e.g. a
                // misconfigured AI provider) in "Could not resolve type";
                // the wrapped exception carries the useful message.
                Exception cause = exception is CommandRuntimeException { InnerException: { } inner }
                    ? inner
                    : exception;

                if (cause is WorkspaceNotInitializedException or CommandRuntimeException or AiException)
                {
                    console.MarkupLineInterpolated($"[red]{cause.Message}[/]");
                }
                else
                {
                    console.WriteException(cause, ExceptionFormats.ShortenEverything);
                }

                return 1;
            });
        });

        return app;
    }

    /// <summary>
    /// Configuration sources, later ones winning: appsettings.json next to
    /// the binary, user secrets (id "recall-commander"), then a gitignored
    /// .env file in the working directory (e.g. Ai__Provider=ollama,
    /// Ai__Gemini__ApiKey=...). Secrets never live in committed files.
    /// </summary>
    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets(typeof(CommandAppFactory).Assembly, optional: true)
            .AddDotEnvFile(Path.Combine(Environment.CurrentDirectory, ".env"))
            .Build();
}
