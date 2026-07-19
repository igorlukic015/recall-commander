using RecallCommander.Application.Artifacts;
using RecallCommander.Contracts.Artifacts;
using Xunit;

namespace RecallCommander.Application.Tests.Artifacts;

public sealed class ArtifactWriterTests
{
    private sealed record TestArtifact(string Title);

    private sealed class TestRenderer : IArtifactRenderer<TestArtifact>
    {
        public string Slug => "my-assessment";

        public string DirectoryName => "TestArtifacts";

        public string Render(TestArtifact artifact, string artifactId) =>
            $"id: {artifactId}\n# {artifact.Title}\n";
    }

    private sealed class RecordingStore : IArtifactStore
    {
        public string? DirectoryPath { get; private set; }

        public string? FileNameStem { get; private set; }

        public string? Markdown { get; private set; }

        public Task<string> SaveAsync(
            string directoryPath,
            string fileNameStem,
            Func<string, string> renderMarkdown,
            CancellationToken cancellationToken = default)
        {
            DirectoryPath = directoryPath;
            FileNameStem = fileNameStem;
            Markdown = renderMarkdown($"{fileNameStem}-001");
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
    public async Task Names_renders_with_the_assigned_id_and_persists_the_artifact()
    {
        RecordingStore store = new RecordingStore();
        ArtifactWriter<TestArtifact> writer = new ArtifactWriter<TestArtifact>(
            new TestRenderer(),
            store,
            new FixedOutputPath("/output"),
            new ArtifactFileNameGenerator(),
            new FixedTimeProvider(new DateTimeOffset(2026, 7, 16, 19, 30, 0, TimeSpan.Zero)));

        SavedArtifact saved = await writer.WriteAsync(new TestArtifact("My Assessment"));

        Assert.Equal(Path.Combine("/output", "TestArtifacts"), store.DirectoryPath);
        Assert.Equal("my-assessment-2026-07-16", store.FileNameStem);
        Assert.Equal("id: my-assessment-2026-07-16-001\n# My Assessment\n", store.Markdown);
        Assert.Equal(
            Path.Combine("/output", "TestArtifacts", "my-assessment-2026-07-16-001.md"),
            saved.FilePath);
    }
}
