namespace RecallCommander.Application.Abstractions;

/// <summary>
/// Filesystem access required by application workflows.
/// </summary>
public interface IFileSystem
{
    /// <summary>Expands and resolves a user-supplied path to an absolute path.</summary>
    string NormalizePath(string path);

    bool DirectoryExists(string path);

    bool FileExists(string path);

    /// <summary>Recursively enumerates Markdown files below a directory, in a stable order.</summary>
    IEnumerable<string> EnumerateMarkdownFiles(string directoryPath);

    string ReadAllText(string filePath);
}
