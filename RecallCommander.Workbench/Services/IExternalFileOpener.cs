namespace RecallCommander.Workbench.Services;

/// <summary>
/// Opens a file in the operating system's default application. Markdown
/// artifacts are user-owned documents; the Workbench never edits them, it
/// hands them to the user's own editor.
/// </summary>
public interface IExternalFileOpener
{
    void Open(string filePath);
}
