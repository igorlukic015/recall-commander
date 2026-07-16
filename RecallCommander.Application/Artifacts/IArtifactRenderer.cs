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
/// The rendered form of an artifact: its complete Markdown document and a
/// slug used to derive the file name.
/// </summary>
public sealed record ArtifactContent
{
    public ArtifactContent(string slug, string markdown)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(markdown);
        Slug = slug;
        Markdown = markdown;
    }

    /// <summary>Human-readable name stem, e.g. "csharp-internals-assessment".</summary>
    public string Slug { get; }

    /// <summary>The full document: YAML frontmatter followed by the Markdown body.</summary>
    public string Markdown { get; }
}
