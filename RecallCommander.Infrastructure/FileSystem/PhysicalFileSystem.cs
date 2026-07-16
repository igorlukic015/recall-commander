using RecallCommander.Application.Abstractions;

namespace RecallCommander.Infrastructure.FileSystem;

public sealed class PhysicalFileSystem : IFileSystem
{
    private static readonly EnumerationOptions Recursive = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
    };

    public string NormalizePath(string path)
    {
        var expanded = ExpandHome(path.Trim());
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(expanded));
    }

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public IEnumerable<string> EnumerateMarkdownFiles(string directoryPath) =>
        Directory.EnumerateFiles(directoryPath, "*.md", Recursive)
            .Order(StringComparer.Ordinal);

    public string ReadAllText(string filePath) => File.ReadAllText(filePath);

    private static string ExpandHome(string path)
    {
        if (path == "~" || path.StartsWith("~/", StringComparison.Ordinal))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, path.TrimStart('~', '/'));
        }

        return path;
    }
}
