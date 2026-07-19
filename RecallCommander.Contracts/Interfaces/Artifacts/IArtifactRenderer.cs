namespace RecallCommander.Contracts.Artifacts;

/// <summary>
/// Renders one artifact type (Assessment, Attempt, Review, ...) to Markdown.
/// Implementations live in the Markdown project and must not touch the
/// filesystem; persistence is handled separately by <see cref="IArtifactStore"/>.
/// </summary>
public interface IArtifactRenderer<in T>
{
    /// <summary>File name stem for this artifact type, e.g. "assessment".</summary>
    string Slug { get; }

    /// <summary>Subdirectory of the artifact output root, e.g. "Assessments".</summary>
    string DirectoryName { get; }

    /// <summary>
    /// Renders the complete document: YAML frontmatter followed by the
    /// Markdown body. The artifact id is the file name the store assigned
    /// (without extension, e.g. "assessment-2026-07-17-001") so generated
    /// artifacts carry their identity in frontmatter and can reference each
    /// other without depending on filenames or database records.
    /// </summary>
    string Render(T artifact, string artifactId);
}
