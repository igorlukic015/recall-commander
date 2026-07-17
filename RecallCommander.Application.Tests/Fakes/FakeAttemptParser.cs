using RecallCommander.Application.Abstractions;

namespace RecallCommander.Application.Tests.Fakes;

public sealed class FakeAttemptParser(AttemptParseResult result) : IAttemptParser
{
    public string? LastMarkdown { get; private set; }

    public AttemptParseResult Parse(string markdown)
    {
        LastMarkdown = markdown;
        return result;
    }
}
