using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace RecallCommander.Workbench.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnLightModeClick(object? sender, RoutedEventArgs e) => SetTheme(ThemeVariant.Light);

    private void OnDarkModeClick(object? sender, RoutedEventArgs e) => SetTheme(ThemeVariant.Dark);

    private void OnExitClick(object? sender, RoutedEventArgs e) => Close();

    private static void SetTheme(ThemeVariant variant)
    {
        if (Avalonia.Application.Current is { } application)
        {
            application.RequestedThemeVariant = variant;
        }
    }
}
