namespace RecallCommander.Infrastructure.Database;

/// <summary>Resolves where Recall Commander keeps its application data.</summary>
public interface IDataPaths
{
    string DataDirectory { get; }

    string DatabasePath { get; }
}

/// <summary>
/// Stores data under the per-user application data directory
/// (e.g. ~/.local/share/RecallCommander). The RC_DATA_DIR environment
/// variable overrides the location.
/// </summary>
public sealed class DataPaths : IDataPaths
{
    public const string DataDirectoryVariable = "RC_DATA_DIR";

    public DataPaths()
    {
        DataDirectory = Environment.GetEnvironmentVariable(DataDirectoryVariable)
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RecallCommander");
        DatabasePath = Path.Combine(DataDirectory, "recall-commander.db");
    }

    public string DataDirectory { get; }

    public string DatabasePath { get; }
}
