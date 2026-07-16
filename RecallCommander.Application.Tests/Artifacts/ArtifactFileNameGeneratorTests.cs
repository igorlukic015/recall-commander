using Xunit;
using RecallCommander.Application.Artifacts;

namespace RecallCommander.Application.Tests.Artifacts;

public sealed class ArtifactFileNameGeneratorTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 7, 16, 19, 30, 0, TimeSpan.Zero);

    private readonly ArtifactFileNameGenerator _generator = new();

    [Fact]
    public void Stem_combines_slug_and_date()
    {
        var stem = _generator.CreateStem("assessment", Timestamp);

        Assert.Equal("assessment-2026-07-16", stem);
    }

    [Theory]
    [InlineData(1, "assessment-2026-07-16-001.md")]
    [InlineData(12, "assessment-2026-07-16-012.md")]
    [InlineData(123, "assessment-2026-07-16-123.md")]
    [InlineData(1234, "assessment-2026-07-16-1234.md")]
    public void Numbered_file_name_appends_three_digit_sequence(int sequence, string expected)
    {
        var fileName = _generator.CreateNumberedFileName("assessment-2026-07-16", sequence);

        Assert.Equal(expected, fileName);
    }

    [Fact]
    public void Rejects_sequence_below_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _generator.CreateNumberedFileName("assessment", 0));
    }

    [Theory]
    [InlineData("C# Internals Assessment", "c-internals-assessment")]
    [InlineData("  spaced   out  ", "spaced-out")]
    [InlineData("snake_case_slug", "snake-case-slug")]
    [InlineData("UPPER", "upper")]
    [InlineData("--already--dashed--", "already-dashed")]
    public void Sanitizes_slug_to_filesystem_safe_form(string slug, string expectedStem)
    {
        var stem = _generator.CreateStem(slug, Timestamp);

        Assert.Equal($"{expectedStem}-2026-07-16", stem);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("///???")]
    public void Falls_back_when_slug_has_no_usable_characters(string slug)
    {
        var stem = _generator.CreateStem(slug, Timestamp);

        Assert.Equal("artifact-2026-07-16", stem);
    }
}
