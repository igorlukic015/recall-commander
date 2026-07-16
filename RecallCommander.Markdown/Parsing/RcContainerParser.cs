using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Syntax;

namespace RecallCommander.Markdown.Parsing;

/// <summary>
/// Block parser for ':::rc-*' containers.
///
/// Recall Commander blocks nest with equal-length fences and a bare ':::' always
/// closes the innermost open block — Markdig's stock custom containers close the
/// outermost block instead, which is why this dedicated parser exists. A closing
/// fence is deferred to a descendant when one is still open that can consume it
/// (a nested rc container, or a fenced code block holding ':::' as content).
/// </summary>
public sealed class RcContainerParser : BlockParser
{
    private const int FenceLength = 3;

    public RcContainerParser()
    {
        OpeningCharacters = [':'];
    }

    public override BlockState TryOpen(BlockProcessor processor)
    {
        if (processor.IsCodeIndent)
        {
            return BlockState.None;
        }

        var line = processor.Line;
        if (CountFenceChars(ref line) < FenceLength)
        {
            return BlockState.None;
        }

        var name = line.ToString().Trim();
        if (!name.StartsWith("rc-", StringComparison.Ordinal))
        {
            // Not a Recall Commander block; leave the line to other parsers.
            return BlockState.None;
        }

        processor.NewBlocks.Push(new RcContainerBlock(this)
        {
            Name = name,
            Column = processor.Column,
            Span = new SourceSpan(processor.Start, processor.Line.End),
            Line = processor.LineIndex,
        });

        return BlockState.ContinueDiscard;
    }

    public override BlockState TryContinue(BlockProcessor processor, Block block)
    {
        if (processor.IsCodeIndent || !IsClosingFence(processor.Line))
        {
            return BlockState.Continue;
        }

        // Defer to the still-open block below us in the stack when it can consume
        // the fence: a nested rc container closing itself first, or a fenced code
        // block holding ':::' as literal content.
        if (processor.NextContinue is RcContainerBlock or FencedCodeBlock)
        {
            return BlockState.Continue;
        }

        block.UpdateSpanEnd(processor.Line.End);
        return BlockState.BreakDiscard;
    }

    private static bool IsClosingFence(StringSlice line)
    {
        return CountFenceChars(ref line) >= FenceLength && line.IsEmptyOrWhitespace();
    }

    private static int CountFenceChars(ref StringSlice line)
    {
        var count = 0;
        while (line.CurrentChar == ':')
        {
            count++;
            line.SkipChar();
        }

        return count;
    }
}
