using System.Text;

namespace RecallCommander.Markdown.Writing;

/// <summary>
/// Composes a Markdown artifact document: optional YAML frontmatter followed
/// by body blocks separated by blank lines. Artifact renderers (Assessment,
/// Attempt, Review, ...) use this so every generated document is formatted
/// consistently.
/// </summary>
public sealed class MarkdownArtifactBuilder
{
    private readonly StringBuilder _body = new();
    private string? _frontmatter;

    /// <summary>Sets the frontmatter object serialized as YAML (see <see cref="YamlFrontmatterSerializer"/>).</summary>
    public MarkdownArtifactBuilder WithFrontmatter(object frontmatter)
    {
        _frontmatter = YamlFrontmatterSerializer.Serialize(frontmatter);
        return this;
    }

    public MarkdownArtifactBuilder AppendHeading(int level, string text)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, 6);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return AppendBlock($"{new string('#', level)} {text.Trim()}");
    }

    /// <summary>Appends a block of Markdown content (a paragraph, list, code fence, ...).</summary>
    public MarkdownArtifactBuilder AppendMarkdown(string markdown)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(markdown);
        return AppendBlock(markdown.Trim());
    }

    public MarkdownArtifactBuilder AppendThematicBreak() => AppendBlock("---");

    public string Build()
    {
        var document = new StringBuilder();

        if (_frontmatter is not null)
        {
            document.Append(_frontmatter).Append('\n');
        }

        document.Append(_body);
        return document.ToString();
    }

    private MarkdownArtifactBuilder AppendBlock(string block)
    {
        if (_body.Length > 0)
        {
            _body.Append('\n');
        }

        _body.Append(block).Append('\n');
        return this;
    }
}
