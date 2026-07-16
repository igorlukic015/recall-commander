using Markdig.Parsers;
using Markdig.Syntax;

namespace RecallCommander.Markdown.Parsing;

/// <summary>
/// A ':::' fenced Recall Commander container (rc-question, rc-prompt, rc-answer, ...).
/// </summary>
public sealed class RcContainerBlock(BlockParser parser) : ContainerBlock(parser)
{
    /// <summary>The name following the opening fence, e.g. "rc-question".</summary>
    public required string Name { get; init; }
}
