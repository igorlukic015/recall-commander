using Xunit;
using RecallCommander.Application.Artifacts;

namespace RecallCommander.Application.Tests.Artifacts;

public sealed class ArtifactFileNameGeneratorTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 7, 16, 19, 30, 0, TimeSpan.Zero);

    private readonly ArtifactFileNameGenerator _generator = new();

    [Fact]
    public void Combines_slug_timestamp_and_extension()
    {
        var fileName = _generator.Create("assessment", Timestamp);

        Assert.Equal("assessment-20260716-193000.md", fileName);
    }

    [Theory]
    [InlineData("C# Internals Assessment", "c-internals-assessment")]
    [InlineData("  spaced   out  ", "spaced-out")]
    [InlineData("snake_case_slug", "snake-case-slug")]
    [InlineData("UPPER", "upper")]
    [InlineData("--already--dashed--", "already-dashed")]
    public void Sanitizes_slug_to_filesystem_safe_form(string slug, string expectedStem)
    {
        var fileName = _generator.Create(slug, Timestamp);

        Assert.Equal($"{expectedStem}-20260716-193000.md", fileName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("///???")]
    public void Falls_back_when_slug_has_no_usable_characters(string slug)
    {
        var fileName = _generator.Create(slug, Timestamp);

        Assert.Equal("artifact-20260716-193000.md", fileName);
    }
}
