using Microsoft.Data.Sqlite;

namespace RecallCommander.IntegrationTests.Support;

/// <summary>
/// A self-contained temporary directory tree for one test:
///
///   {temp}/recall-commander-tests/{id}/
///       Questions/     question source files
///       .rc/           SQLite database
///       Assessments/   generated artifacts (created by the code under test)
///
/// Everything is created fresh per test and deleted on dispose.
/// </summary>
public sealed class TestWorkspace : IDisposable
{
    public TestWorkspace()
    {
        Root = Path.Combine(
            Path.GetTempPath(),
            "recall-commander-tests",
            Guid.NewGuid().ToString("N"));
        QuestionsDirectory = Path.Combine(Root, "Questions");
        DataDirectory = Path.Combine(Root, ".rc");

        Directory.CreateDirectory(QuestionsDirectory);
        Directory.CreateDirectory(DataDirectory);
    }

    public string Root { get; }

    public string QuestionsDirectory { get; }

    public string DataDirectory { get; }

    public string AssessmentsDirectory => Path.Combine(Root, "Assessments");

    public string ReviewsDirectory => Path.Combine(Root, "Reviews");

    /// <summary>Writes a Markdown file below Questions/, creating subdirectories as needed.</summary>
    public string WriteQuestionFile(string relativePath, string content)
    {
        string path = Path.Combine(QuestionsDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>Creates an additional source directory next to Questions/.</summary>
    public string CreateSourceDirectory(string name)
    {
        string path = Path.Combine(Root, name);
        Directory.CreateDirectory(path);
        return path;
    }

    public string WriteFile(string absoluteDirectory, string fileName, string content)
    {
        string path = Path.Combine(absoluteDirectory, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose()
    {
        // Release pooled SQLite handles so the database file can be removed.
        SqliteConnection.ClearAllPools();

        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Best effort; the OS temp directory is cleaned eventually.
        }
    }
}
