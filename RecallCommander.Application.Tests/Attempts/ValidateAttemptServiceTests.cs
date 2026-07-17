using Xunit;
using RecallCommander.Application.Attempts;
using RecallCommander.Application.Tests.Fakes;
using RecallCommander.Contracts.Attempts;
using RecallCommander.Contracts.Parsing;
using RecallCommander.Domain;

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
        var parser = new FakeAttemptParser(new AttemptParseResult(SampleAttempt(), []));
        var service = new ValidateAttemptService(new FakeFileSystem(), parser);

        var result = service.Validate("attempts/missing.md");

        Assert.Equal(ValidateAttemptStatus.FileNotFound, result.Status);
        Assert.Equal("attempts/missing.md", result.FilePath);
        Assert.Null(parser.LastMarkdown);
    }

    [Fact]
    public void Parses_the_file_content_and_returns_the_attempt()
    {
        var attempt = SampleAttempt();
        var parser = new FakeAttemptParser(new AttemptParseResult(attempt, []));
        var fileSystem = new FakeFileSystem().AddFile("attempts/done.md", "the document");
        var service = new ValidateAttemptService(fileSystem, parser);

        var result = service.Validate("attempts/done.md");

        Assert.Equal(ValidateAttemptStatus.Valid, result.Status);
        Assert.Same(attempt, result.Attempt);
        Assert.Equal("the document", parser.LastMarkdown);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Passes_parser_diagnostics_through_when_the_document_is_invalid()
    {
        var diagnostics = new[]
        {
            new ParseDiagnostic(7, "Missing '### Answer' heading."),
            new ParseDiagnostic(13, "Question has no prompt text."),
        };
        var parser = new FakeAttemptParser(new AttemptParseResult(Attempt: null, diagnostics));
        var fileSystem = new FakeFileSystem().AddFile("attempts/broken.md", "the document");
        var service = new ValidateAttemptService(fileSystem, parser);

        var result = service.Validate("attempts/broken.md");

        Assert.Equal(ValidateAttemptStatus.Invalid, result.Status);
        Assert.Null(result.Attempt);
        Assert.Equal(diagnostics, result.Diagnostics);
    }
}
