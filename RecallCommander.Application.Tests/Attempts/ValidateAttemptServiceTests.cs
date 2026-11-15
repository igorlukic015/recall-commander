using RecallCommander.Application.Attempts;
using RecallCommander.Application.Tests.Fakes;
using RecallCommander.Contracts.Attempts;
using RecallCommander.Contracts.Parsing;
using RecallCommander.Domain;
using Xunit;

namespace RecallCommander.Application.Tests.Attempts;

public sealed class ValidateAttemptServiceTests
{
    private static Attempt SampleAttempt() => new(
        "C# Assessment",
        new DateTimeOffset(2026, 7, 17, 18, 0, 0, TimeSpan.Zero),
        [new AttemptQuestion("What is boxing?", "An answer.")]);

    [Fact]
    public void Reports_a_missing_file()
    {
        FakeAttemptParser parser = new FakeAttemptParser(new AttemptParseResult(SampleAttempt(), []));
        ValidateAttemptService service = new ValidateAttemptService(new FakeFileSystem(), parser);

        ValidateAttemptResult result = service.Validate("attempts/missing.md");

        Assert.Equal(ValidateAttemptStatus.FileNotFound, result.Status);
        Assert.Equal("attempts/missing.md", result.FilePath);
        Assert.Null(parser.LastMarkdown);
    }

    [Fact]
    public void Parses_the_file_content_and_returns_the_attempt()
    {
        Attempt attempt = SampleAttempt();
        FakeAttemptParser parser = new FakeAttemptParser(new AttemptParseResult(attempt, []));
        FakeFileSystem fileSystem = new FakeFileSystem().AddFile("attempts/done.md", "the document");
        ValidateAttemptService service = new ValidateAttemptService(fileSystem, parser);

        ValidateAttemptResult result = service.Validate("attempts/done.md");

        Assert.Equal(ValidateAttemptStatus.Valid, result.Status);
        Assert.Same(attempt, result.Attempt);
        Assert.Equal("the document", parser.LastMarkdown);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Passes_parser_diagnostics_through_when_the_document_is_invalid()
    {
        ParseDiagnostic[] diagnostics = new[]
        {
            new ParseDiagnostic(7, "Missing '### Answer' heading."),
            new ParseDiagnostic(13, "Question has no prompt text."),
        };
        FakeAttemptParser parser = new FakeAttemptParser(new AttemptParseResult(Attempt: null, diagnostics));
        FakeFileSystem fileSystem = new FakeFileSystem().AddFile("attempts/broken.md", "the document");
        ValidateAttemptService service = new ValidateAttemptService(fileSystem, parser);

        ValidateAttemptResult result = service.Validate("attempts/broken.md");

        Assert.Equal(ValidateAttemptStatus.Invalid, result.Status);
        Assert.Null(result.Attempt);
        Assert.Equal(diagnostics, result.Diagnostics);
    }
}
