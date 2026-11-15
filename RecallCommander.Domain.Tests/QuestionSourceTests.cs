using RecallCommander.Domain;
using Xunit;

namespace RecallCommander.Domain.Tests;

public sealed class QuestionSourceTests
{
    [Fact]
    public void Creates_source()
    {
        DateTimeOffset registeredAt = new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);

        QuestionSource source = new QuestionSource(1, "/home/user/questions", registeredAt);

        Assert.Equal(1, source.Id);
        Assert.Equal("/home/user/questions", source.DirectoryPath);
        Assert.Equal(registeredAt, source.RegisteredAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_empty_path(string path)
    {
        Assert.Throws<DomainException>(() =>
            new QuestionSource(1, path, DateTimeOffset.UtcNow));
    }
}
