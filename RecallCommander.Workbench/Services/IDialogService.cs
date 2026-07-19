namespace RecallCommander.Workbench.Services;

/// <summary>UI-only abstraction over the platform file and folder pickers.</summary>
public interface IDialogService
{
    /// <summary>Asks the user to pick a directory. Returns null when cancelled.</summary>
    Task<string?> PickFolderAsync(string title);

    /// <summary>Asks the user to pick a single file. Returns null when cancelled.</summary>
    Task<string?> PickFileAsync(string title, string filterName, IReadOnlyList<string> patterns);
}
