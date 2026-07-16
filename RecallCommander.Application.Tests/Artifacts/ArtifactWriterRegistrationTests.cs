using Xunit;
using Microsoft.Extensions.DependencyInjection;
using RecallCommander.Application.Artifacts;

namespace RecallCommander.Application.Tests.Artifacts;

public sealed class ArtifactWriterRegistrationTests
{
    private sealed record TestArtifact;

    private sealed class TestRenderer : IArtifactRenderer<TestArtifact>
    {
        public ArtifactContent Render(TestArtifact artifact) => new("test", "Tests", "# Test\n");
    }

    private sealed class NoopStore : IArtifactStore
    {
        public Task<string> SaveAsync(
            string directoryPath,
            string fileNameStem,
            string markdown,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Path.Combine(directoryPath, $"{fileNameStem}-001.md"));
    }

    private sealed class NoopOutputPath : IArtifactOutputPathProvider
    {
        public string GetOutputDirectory() => "/output";
    }

    [Fact]
    public void Registering_a_renderer_is_all_a_new_artifact_type_needs()
    {
        var services = new ServiceCollection()
            .AddRecallCommanderApplication()
            .AddSingleton<IArtifactStore, NoopStore>()
            .AddSingleton<IArtifactOutputPathProvider, NoopOutputPath>()
            .AddSingleton<IArtifactRenderer<TestArtifact>, TestRenderer>();

        using var provider = services.BuildServiceProvider();

        var writer = provider.GetService<IArtifactWriter<TestArtifact>>();

        Assert.NotNull(writer);
    }
}
