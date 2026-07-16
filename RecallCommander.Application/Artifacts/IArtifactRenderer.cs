namespace RecallCommander.Application.Artifacts;

/// <summary>
/// Renders one artifact type (Assessment, Attempt, Review, ...) to Markdown.
/// Implementations live in the Markdown project and must not touch the
/// filesystem; persistence is handled separately by <see cref="IArtifactStore"/>.
/// </summary>
public interface IArtifactRenderer<in T>
{
    ArtifactContent Render(T artifact);
}

/// <summary>
/// The rendered form of an artifact: its complete Markdown document, a slug
/// used to derive the file name, and the directory this artifact type lives in.
/// </summary>
public sealed record ArtifactContent
{
    public ArtifactContent(string slug, string directoryName, string markdown)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryName);
        ArgumentException.ThrowIfNullOrWhiteSpace(markdown);
        Slug = slug;
        DirectoryName = directoryName;
        Markdown = markdown;
    }

    /// <summary>File name stem, e.g. "assessment".</summary>
    public string Slug { get; }

    /// <summary>Subdirectory of the artifact output root, e.g. "Assessments".</summary>
    public string DirectoryName { get; }

    /// <summary>The full document: YAML frontmatter followed by the Markdown body.</summary>
    public string Markdown { get; }
}
