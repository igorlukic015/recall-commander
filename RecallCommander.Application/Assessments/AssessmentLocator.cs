using RecallCommander.Contracts.Artifacts;
using RecallCommander.Contracts.FileSystem;
using RecallCommander.Domain;

namespace RecallCommander.Application.Assessments;

/// <summary>
/// Discovers generated assessment artifacts for display. The Markdown files
/// are the source of truth — there is no database of assessments — so this
/// simply lists the artifact output directory, newest first. Callers only
/// ever read these files; editing happens in the user's own editor.
/// </summary>
public sealed class AssessmentLocator(
    IArtifactOutputPathProvider outputPath,
    IArtifactRenderer<Assessment> renderer,
    IFileSystem fileSystem)
{
    public IReadOnlyList<ArtifactFile> List()
    {
        string directory = Path.Combine(outputPath.GetOutputDirectory(), renderer.DirectoryName);

        if (!fileSystem.DirectoryExists(directory))
        {
            return [];
        }

        // Artifact names embed date and sequence number, so ordinal descending
        // file name order is newest first.
        return fileSystem.EnumerateMarkdownFiles(directory)
            .Select(path => new ArtifactFile(Path.GetFileName(path), path))
            .OrderByDescending(file => file.FileName, StringComparer.Ordinal)
            .ToList();
    }
}

/// <summary>An artifact Markdown file on disk, listed for display.</summary>
public sealed record ArtifactFile(string FileName, string FilePath);
