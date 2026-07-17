using RecallCommander.Application.Abstractions;

namespace RecallCommander.Application.Tests.Fakes;

public sealed class FakeFileSystem : IFileSystem
{
    private readonly HashSet<string> _directories = [];
    private readonly Dictionary<string, string> _files = [];
    private readonly HashSet<string> _unreadableFiles = [];

    public FakeFileSystem AddDirectory(string path)
    {
        _directories.Add(path);
        return this;
    }

    public FakeFileSystem AddFile(string path, string content)
    {
        _files[path] = content;
        return this;
    }

    public FakeFileSystem AddUnreadableFile(string path)
    {
        _files[path] = string.Empty;
        _unreadableFiles.Add(path);
        return this;
    }

    public string NormalizePath(string path) => path;

    public bool DirectoryExists(string path) => _directories.Contains(path);

    public bool FileExists(string path) => _files.ContainsKey(path);

    public IEnumerable<string> EnumerateMarkdownFiles(string directoryPath) =>
        _files.Keys
            .Where(file => file.StartsWith(directoryPath + "/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal);

    public string ReadAllText(string filePath) =>
        _unreadableFiles.Contains(filePath)
            ? throw new IOException($"Cannot read '{filePath}'.")
            : _files[filePath];
}
