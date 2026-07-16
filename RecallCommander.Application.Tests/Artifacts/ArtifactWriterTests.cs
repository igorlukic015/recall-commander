using Xunit;
using RecallCommander.Application.Artifacts;

namespace RecallCommander.Application.Tests.Artifacts;

public sealed class ArtifactWriterTests
{
    private sealed record TestArtifact(string Title);

    private sealed class TestRenderer : IArtifactRenderer<TestArtifact>
    {
        public ArtifactContent Render(TestArtifact artifact) =>
            new(artifact.Title, "TestArtifacts", $"# {artifact.Title}\n");
    }

    private sealed class RecordingStore : IArtifactStore
    {
        public string? DirectoryPath { get; private set; }

        public string? FileNameStem { get; private set; }

        public string? Markdown { get; private set; }

        public Task<string> SaveAsync(
            string directoryPath,
            string fileNameStem,
            string markdown,
            CancellationToken cancellationToken = default)
        {
            DirectoryPath = directoryPath;
            FileNameStem = fileNameStem;
            Markdown = markdown;
            return Task.FromResult(Path.Combine(directoryPath, $"{fileNameStem}-001.md"));
        }
    }

    private sealed class FixedOutputPath(string directory) : IArtifactOutputPathProvider
    {
        public string GetOutputDirectory() => directory;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public async Task Renders_names_and_persists_the_artifact()
    {
        var store = new RecordingStore();
        var writer = new ArtifactWriter<TestArtifact>(
            new TestRenderer(),
            store,
            new FixedOutputPath("/output"),
            new ArtifactFileNameGenerator(),
            new FixedTimeProvider(new DateTimeOffset(2026, 7, 16, 19, 30, 0, TimeSpan.Zero)));

        var saved = await writer.WriteAsync(new TestArtifact("My Assessment"));

        Assert.Equal(Path.Combine("/output", "TestArtifacts"), store.DirectoryPath);
        Assert.Equal("my-assessment-2026-07-16", store.FileNameStem);
        Assert.Equal("# My Assessment\n", store.Markdown);
        Assert.Equal(
            Path.Combine("/output", "TestArtifacts", "my-assessment-2026-07-16-001.md"),
            saved.FilePath);
    }
}
