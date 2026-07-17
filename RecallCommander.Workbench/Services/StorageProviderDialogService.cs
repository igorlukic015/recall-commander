using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace RecallCommander.Workbench.Services;

/// <summary>
/// Implements dialogs with Avalonia's storage provider. The top level is
/// resolved lazily because the main window does not exist yet when the
/// service collection is built.
/// </summary>
public sealed class StorageProviderDialogService(Func<TopLevel?> topLevelAccessor) : IDialogService
{
    public async Task<string?> PickFolderAsync(string title)
    {
        if (topLevelAccessor() is not { } topLevel)
        {
            return null;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickFileAsync(string title, string filterName, IReadOnlyList<string> patterns)
    {
        if (topLevelAccessor() is not { } topLevel)
        {
            return null;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType(filterName) { Patterns = [.. patterns] },
                FilePickerFileTypes.All,
            ],
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }
}
