using Markdig;
using Markdig.Syntax;
using RecallCommander.Contracts.Parsing;
using RecallCommander.Contracts.Questions;
using RecallCommander.Domain;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RecallCommander.Markdown.Parsing;

/// <summary>
/// Finds ':::rc-question' containers in a Markdown document and turns the valid
/// ones into questions. Everything outside Question Blocks is ignored. Invalid
/// blocks are skipped with a diagnostic; parsing never stops early.
/// </summary>
public sealed class QuestionBlockParser : IQuestionBlockParser
{
    private const string QuestionInfo = "rc-question";
    private const string PromptInfo = "rc-prompt";
    private const string AnswerInfo = "rc-answer";

    private static readonly MarkdownPipeline Pipeline = CreatePipeline();

    private static readonly IDeserializer MetadataDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static MarkdownPipeline CreatePipeline()
    {
        MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder();
        builder.BlockParsers.Insert(0, new RcContainerParser());
        return builder.Build();
    }

    public QuestionParseResult Parse(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        MarkdownDocument document = Markdig.Markdown.Parse(markdown, Pipeline);

        List<DiscoveredQuestion> questions = new List<DiscoveredQuestion>();
        List<ParseDiagnostic> diagnostics = new List<ParseDiagnostic>();

        foreach (Block block in document)
        {
            if (block is not RcContainerBlock container)
            {
                continue;
            }

            switch (Info(container))
            {
                case QuestionInfo:
                    ParseQuestionBlock(container, markdown, questions, diagnostics);
                    break;

                case PromptInfo or AnswerInfo:
                    diagnostics.Add(new ParseDiagnostic(
                        LineOf(container),
                        $"':::{Info(container)}' block found outside an rc-question block."));
                    break;
            }
        }

        return new QuestionParseResult(questions, diagnostics);
    }

    private static void ParseQuestionBlock(
        RcContainerBlock questionContainer,
        string markdown,
        List<DiscoveredQuestion> questions,
        List<ParseDiagnostic> diagnostics)
    {
        int questionLine = LineOf(questionContainer);
        List<ParseDiagnostic> errors = new List<ParseDiagnostic>();

        List<Block> metadataBlocks = new List<Block>();
        RcContainerBlock? promptContainer = null;
        RcContainerBlock? answerContainer = null;

        foreach (Block child in questionContainer)
        {
            if (child is RcContainerBlock nested)
            {
                switch (Info(nested))
                {
                    case PromptInfo:
                        CollectNested(nested, ref promptContainer, PromptInfo, errors);
                        break;

                    case AnswerInfo:
                        CollectNested(nested, ref answerContainer, AnswerInfo, errors);
                        break;

                    case QuestionInfo:
                        errors.Add(new ParseDiagnostic(
                            LineOf(nested),
                            "Nested rc-question block. A previous block may be missing its closing ':::'."));
                        break;

                        // Unknown containers are ignored so future block types do not
                        // invalidate existing questions.
                }
            }
            else if (promptContainer is null && answerContainer is null)
            {
                // Metadata is everything directly inside rc-question before the
                // first nested block.
                metadataBlocks.Add(child);
            }
        }

        QuestionType? type = ParseMetadata(metadataBlocks, markdown, questionLine, errors, out IReadOnlyList<string>? concepts);

        string? prompt = null;
        if (promptContainer is null)
        {
            errors.Add(new ParseDiagnostic(questionLine, "Missing rc-prompt block."));
        }
        else
        {
            prompt = RawText(promptContainer, markdown).Trim();
            if (prompt.Length == 0)
            {
                errors.Add(new ParseDiagnostic(LineOf(promptContainer), "rc-prompt block is empty."));
            }
        }

        string? answer = answerContainer is null ? null : RawText(answerContainer, markdown).Trim();
        if (answer is { Length: 0 })
        {
            answer = null;
        }

        if (errors.Count > 0)
        {
            diagnostics.AddRange(errors);
            return;
        }

        questions.Add(new DiscoveredQuestion(
            new Question(type!.Value, prompt!, answer, concepts),
            questionLine));
    }

    private static void CollectNested(
        RcContainerBlock nested,
        ref RcContainerBlock? slot,
        string info,
        List<ParseDiagnostic> errors)
    {
        if (slot is not null)
        {
            errors.Add(new ParseDiagnostic(LineOf(nested), $"Duplicate {info} block."));
            return;
        }

        slot = nested;
    }

    private static QuestionType? ParseMetadata(
        List<Block> metadataBlocks,
        string markdown,
        int questionLine,
        List<ParseDiagnostic> errors,
        out IReadOnlyList<string> concepts)
    {
        concepts = [];

        QuestionMetadata? metadata = null;
        if (metadataBlocks.Count > 0)
        {
            string yaml = RawText(metadataBlocks[0], metadataBlocks[^1], markdown);
            try
            {
                metadata = MetadataDeserializer.Deserialize<QuestionMetadata>(yaml);
            }
            catch (YamlException exception)
            {
                int line = metadataBlocks[0].Line + (int)exception.Start.Line;
                errors.Add(new ParseDiagnostic(line, $"Invalid metadata: {exception.InnerException?.Message ?? exception.Message}"));
                return null;
            }
        }

        if (string.IsNullOrWhiteSpace(metadata?.Type))
        {
            errors.Add(new ParseDiagnostic(questionLine, "Missing required field 'type'."));
            return null;
        }

        if (!Enum.TryParse<QuestionType>(metadata.Type, ignoreCase: true, out QuestionType type))
        {
            errors.Add(new ParseDiagnostic(
                questionLine,
                $"Unknown question type '{metadata.Type}'. Supported types: {string.Join(", ", Enum.GetNames<QuestionType>())}."));
            return null;
        }

        concepts = metadata.Concepts?
            .Where(concept => !string.IsNullOrWhiteSpace(concept))
            .Select(concept => concept.Trim())
            .ToList() ?? [];

        return type;
    }

    private static string Info(RcContainerBlock container) => container.Name;

    private static int LineOf(Block block) => block.Line + 1;

    /// <summary>Extracts the raw source text spanned by a container's children.</summary>
    private static string RawText(ContainerBlock container, string markdown) =>
        container.Count == 0 ? string.Empty : RawText(container[0], container[^1], markdown);

    /// <summary>Extracts the raw source text from the start of one block to the end of another.</summary>
    private static string RawText(Block first, Block last, string markdown)
    {
        int start = first.Span.Start;
        int end = last.Span.End;

        if (start < 0 || end < start || end >= markdown.Length)
        {
            return string.Empty;
        }

        return markdown.Substring(start, end - start + 1);
    }

    private sealed class QuestionMetadata
    {
        public string? Type { get; set; }

        public List<string>? Concepts { get; set; }
    }
}
