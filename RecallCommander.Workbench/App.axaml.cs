using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecallCommander.AI;
using RecallCommander.Application;
using RecallCommander.Infrastructure;
using RecallCommander.Infrastructure.Configuration;
using RecallCommander.Markdown;
using RecallCommander.Workbench.Services;
using RecallCommander.Workbench.ViewModels;
using RecallCommander.Workbench.Views;

namespace RecallCommander.Workbench;

public partial class App : Avalonia.Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _services = BuildServices(desktop);
            MainWindowViewModel viewModel = _services.GetRequiredService<MainWindowViewModel>();

            MainWindow mainWindow = new MainWindow { DataContext = viewModel };
            mainWindow.Opened += (_, _) => _ = viewModel.InitializeAsync();

            desktop.MainWindow = mainWindow;
            desktop.Exit += (_, _) => _services.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Composition root for the Workbench: the same Application, Markdown,
    /// Infrastructure and AI registrations the CLI uses, plus UI-only services.
    /// </summary>
    private static ServiceProvider BuildServices(IClassicDesktopStyleApplicationLifetime desktop)
    {
        ServiceCollection services = new ServiceCollection();
        services.AddRecallCommanderApplication();
        services.AddRecallCommanderMarkdown();
        services.AddRecallCommanderInfrastructure();
        services.AddRecallCommanderAi(BuildConfiguration());

        services.AddSingleton<IDialogService>(new StorageProviderDialogService(() => desktop.MainWindow));
        services.AddSingleton<IExternalFileOpener, ShellFileOpener>();
        services.AddSingleton<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// The same configuration chain as the CLI, later sources winning:
    /// appsettings.json next to the binary, user secrets (id
    /// "recall-commander"), then a gitignored .env file in the working
    /// directory. An AI provider configured for 'rc' works here unchanged.
    /// </summary>
    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets(typeof(App).Assembly, optional: true)
            .AddDotEnvFile(Path.Combine(Environment.CurrentDirectory, ".env"))
            .Build();
}
