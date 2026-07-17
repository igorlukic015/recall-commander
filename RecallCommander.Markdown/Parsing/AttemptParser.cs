using System.Globalization;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using RecallCommander.Contracts.Attempts;
using RecallCommander.Contracts.Parsing;
using RecallCommander.Domain;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RecallCommander.Markdown.Parsing;

/// <summary>
/// Parses a completed assessment document into an <see cref="Attempt"/>.
/// The document structure is the one the assessment renderer produces:
/// YAML frontmatter, '## Question N' sections, and a '### Answer' heading
/// inside each section under which the user wrote an answer. Every problem
/// is collected as a diagnostic; parsing never stops at the first error.
/// </summary>
public sealed class AttemptParser : IAttemptParser
{
    private const string AssessmentType = "assessment";
    private const string QuestionHeadingPrefix = "Question";
    private const string AnswerHeadingText = "Answer";
    private const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseYamlFrontMatter()
        .Build();

    private static readonly IDeserializer FrontmatterDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public AttemptParseResult Parse(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var document = Markdig.Markdown.Parse(markdown, Pipeline);
        var diagnostics = new List<ParseDiagnostic>();

        var createdAt = ParseFrontmatter(document, out var frontmatterTitle, out var assessmentId, diagnostics);
        var questions = ParseQuestionSections(document, markdown, diagnostics, out var sectionCount);

        var title = frontmatterTitle ?? FirstTitleHeading(document);
        if (title is null)
        {
            diagnostics.Add(new ParseDiagnostic(1, "Missing title. Expected a 'title' frontmatter field or a level 1 heading."));
        }

        if (sectionCount == 0)
        {
            diagnostics.Add(new ParseDiagnostic(1, "No questions found. Expected '## Question' headings."));
        }

        if (diagnostics.Count > 0)
        {
            return new AttemptParseResult(Attempt: null, diagnostics);
        }

        return new AttemptParseResult(new Attempt(title!, createdAt!.Value, questions, assessmentId), diagnostics);
    }

    private static DateTimeOffset? ParseFrontmatter(
        MarkdownDocument document,
        out string? title,
        out string? assessmentId,
        List<ParseDiagnostic> diagnostics)
    {
        title = null;
        assessmentId = null;

        if (document.FirstOrDefault() is not YamlFrontMatterBlock yaml)
        {
            diagnostics.Add(new ParseDiagnostic(1, "Missing assessment frontmatter."));
            return null;
        }

        var line = LineOf(yaml);

        AttemptFrontmatter? frontmatter;
        try
        {
            frontmatter = FrontmatterDeserializer.Deserialize<AttemptFrontmatter>(yaml.Lines.ToString());
        }
        catch (YamlException exception)
        {
            diagnostics.Add(new ParseDiagnostic(
                line,
                $"Invalid frontmatter: {exception.InnerException?.Message ?? exception.Message}"));
            return null;
        }

        if (string.IsNullOrWhiteSpace(frontmatter?.Type))
        {
            diagnostics.Add(new ParseDiagnostic(line, "Frontmatter is missing required field 'type'."));
            return null;
        }

        // A Save As does not change the artifact type: an attempt keeps
        // 'type: assessment' and is an attempt because the user passed it
        // to the attempt parser.
        var type = frontmatter.Type.Trim();
        if (!type.Equals(AssessmentType, StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(new ParseDiagnostic(
                line,
                $"Unexpected document type '{type}'. Expected '{AssessmentType}'."));
            return null;
        }

        title = string.IsNullOrWhiteSpace(frontmatter.Title) ? null : frontmatter.Title.Trim();

        // The assessment identity, preserved through Save As: either an
        // explicit 'assessment' reference or the 'id' the generated
        // assessment carried. Both are optional — older documents have
        // neither.
        assessmentId = FirstNonEmpty(frontmatter.Assessment, frontmatter.Id);

        if (string.IsNullOrWhiteSpace(frontmatter.Created))
        {
            diagnostics.Add(new ParseDiagnostic(line, "Frontmatter is missing required field 'created'."));
            return null;
        }

        if (!DateTimeOffset.TryParseExact(
                frontmatter.Created.Trim(),
                TimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var createdAt))
        {
            diagnostics.Add(new ParseDiagnostic(
                line,
                $"Invalid 'created' timestamp '{frontmatter.Created.Trim()}'. Expected the format {TimestampFormat}."));
            return null;
        }

        return createdAt;
    }

    private static List<AttemptQuestion> ParseQuestionSections(
        MarkdownDocument document,
        string markdown,
        List<ParseDiagnostic> diagnostics,
        out int sectionCount)
    {
        var questions = new List<AttemptQuestion>();
        sectionCount = 0;

        QuestionSection? current = null;

        foreach (var block in document)
        {
            switch (block)
            {
                case YamlFrontMatterBlock:
                    continue;

                // Top-level thematic breaks are the separators between
                // questions. They never start or end a prompt or answer, but
                // one sitting between two content blocks stays part of the
                // raw text span, so '---' inside an answer survives.
                case ThematicBreakBlock:
                    continue;

                case HeadingBlock heading when IsQuestionHeading(heading):
                    Complete(current, markdown, questions, diagnostics);
                    current = new QuestionSection(heading);
                    sectionCount++;
                    continue;

                // Any other title-level heading ends the current question;
                // its section is not part of the attempt.
                case HeadingBlock { Level: <= 2 }:
                    Complete(current, markdown, questions, diagnostics);
                    current = null;
                    continue;

                case HeadingBlock heading when IsAnswerHeading(heading):
                    if (current is null)
                    {
                        diagnostics.Add(new ParseDiagnostic(
                            LineOf(heading),
                            "'### Answer' heading found outside a question section."));
                    }
                    else if (!current.HasAnswerHeading)
                    {
                        current.HasAnswerHeading = true;
                    }
                    else
                    {
                        // A second '### Answer' is the user's own Markdown
                        // inside the answer.
                        current.AnswerBlocks.Add(heading);
                    }

                    continue;
            }

            // Everything else is content: part of the prompt before the
            // Answer heading, part of the answer after it, and ignored
            // outside question sections (title, instructions).
            if (current is not null)
            {
                (current.HasAnswerHeading ? current.AnswerBlocks : current.PromptBlocks).Add(block);
            }
        }

        Complete(current, markdown, questions, diagnostics);
        return questions;
    }

    private static void Complete(
        QuestionSection? section,
        string markdown,
        List<AttemptQuestion> questions,
        List<ParseDiagnostic> diagnostics)
    {
        if (section is null)
        {
            return;
        }

        var line = LineOf(section.Heading);
        var valid = true;

        var prompt = RawText(section.PromptBlocks, markdown).Trim();
        if (prompt.Length == 0)
        {
            diagnostics.Add(new ParseDiagnostic(line, "Question has no prompt text."));
            valid = false;
        }

        if (!section.HasAnswerHeading)
        {
            diagnostics.Add(new ParseDiagnostic(line, "Missing '### Answer' heading."));
            valid = false;
        }

        if (valid)
        {
            questions.Add(new AttemptQuestion(prompt, RawText(section.AnswerBlocks, markdown).Trim()));
        }
    }

    private static bool IsQuestionHeading(HeadingBlock heading) =>
        heading.Level == 2
        && HeadingText(heading).StartsWith(QuestionHeadingPrefix, StringComparison.OrdinalIgnoreCase);

    private static bool IsAnswerHeading(HeadingBlock heading) =>
        heading.Level == 3
        && HeadingText(heading).Equals(AnswerHeadingText, StringComparison.OrdinalIgnoreCase);

    private static string? FirstTitleHeading(MarkdownDocument document)
    {
        var heading = document.OfType<HeadingBlock>().FirstOrDefault(block => block.Level == 1);
        if (heading is null)
        {
            return null;
        }

        var text = HeadingText(heading);
        return text.Length == 0 ? null : text;
    }

    private static string HeadingText(HeadingBlock heading) =>
        heading.Inline is null
            ? string.Empty
            : string.Concat(heading.Inline.Descendants<LiteralInline>().Select(literal => literal.Content.ToString())).Trim();

    private static int LineOf(Block block) => block.Line + 1;

    private static string? FirstNonEmpty(params ReadOnlySpan<string?> values)
    {
        foreach (var value in values)
        {
            var trimmed = value?.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                return trimmed;
            }
        }

        return null;
    }

    /// <summary>Extracts the raw source text spanned by a list of blocks.</summary>
    private static string RawText(List<Block> blocks, string markdown) =>
        blocks.Count == 0 ? string.Empty : RawText(blocks[0], blocks[^1], markdown);

    /// <summary>Extracts the raw source text from the start of one block to the end of another.</summary>
    private static string RawText(Block first, Block last, string markdown)
    {
        var start = first.Span.Start;
        var end = last.Span.End;

        if (start < 0 || end < start || end >= markdown.Length)
        {
            return string.Empty;
        }

        return markdown.Substring(start, end - start + 1);
    }

    private sealed class QuestionSection(HeadingBlock heading)
    {
        public HeadingBlock Heading { get; } = heading;

        public List<Block> PromptBlocks { get; } = [];

        public List<Block> AnswerBlocks { get; } = [];

        public bool HasAnswerHeading { get; set; }
    }

    private sealed class AttemptFrontmatter
    {
        public string? Type { get; set; }

        public string? Id { get; set; }

        public string? Assessment { get; set; }

        public string? Title { get; set; }

        public string? Created { get; set; }
    }
}
