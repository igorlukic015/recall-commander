using Xunit;
using RecallCommander.Application.Artifacts;

namespace RecallCommander.Application.Tests.Artifacts;

public sealed class ArtifactWriterTests
{
    private sealed record TestArtifact(string Title);

    private sealed class TestRenderer : IArtifactRenderer<TestArtifact>
    {
        public ArtifactContent Render(TestArtifact artifact) =>
            new(artifact.Title, $"# {artifact.Title}\n");
    }

    private sealed class RecordingStore : IArtifactStore
    {
        public string? DirectoryPath { get; private set; }

        public string? FileName { get; private set; }

        public string? Markdown { get; private set; }

        public Task<string> SaveAsync(
            string directoryPath,
            string fileName,
            string markdown,
            CancellationToken cancellationToken = default)
        {
            DirectoryPath = directoryPath;
            FileName = fileName;
            Markdown = markdown;
            return Task.FromResult(Path.Combine(directoryPath, fileName));
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

        Assert.Equal("/output", store.DirectoryPath);
        Assert.Equal("my-assessment-20260716-193000.md", store.FileName);
        Assert.Equal("# My Assessment\n", store.Markdown);
        Assert.Equal(Path.Combine("/output", "my-assessment-20260716-193000.md"), saved.FilePath);
    }
}
