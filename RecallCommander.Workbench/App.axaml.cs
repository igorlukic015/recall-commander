using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using RecallCommander.Application;
using RecallCommander.Infrastructure;
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
    /// Composition root for the Workbench: the same Application, Markdown and
    /// Infrastructure registrations the CLI uses, plus UI-only services.
    /// </summary>
    private static ServiceProvider BuildServices(IClassicDesktopStyleApplicationLifetime desktop)
    {
        ServiceCollection services = new ServiceCollection();
        services.AddRecallCommanderApplication();
        services.AddRecallCommanderMarkdown();
        services.AddRecallCommanderInfrastructure();

        services.AddSingleton<IDialogService>(new StorageProviderDialogService(() => desktop.MainWindow));
        services.AddSingleton<IExternalFileOpener, ShellFileOpener>();
        services.AddSingleton<AssessmentLocator>();
        services.AddSingleton<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}
