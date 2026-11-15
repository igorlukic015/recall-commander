using System.Diagnostics;

namespace RecallCommander.Workbench.Services;

/// <summary>Opens files through the OS shell (default application association).</summary>
public sealed class ShellFileOpener : IExternalFileOpener
{
    public void Open(string filePath) =>
        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
}
