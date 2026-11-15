using Microsoft.Extensions.DependencyInjection;
using RecallCommander.Application.Artifacts;
using RecallCommander.Contracts.Artifacts;
using Xunit;

namespace RecallCommander.Application.Tests.Artifacts;

public sealed class ArtifactWriterRegistrationTests
{
    private sealed record TestArtifact;

    private sealed class TestRenderer : IArtifactRenderer<TestArtifact>
    {
        public string Slug => "test";

        public string DirectoryName => "Tests";

        public string Render(TestArtifact artifact, string artifactId) => "# Test\n";
    }

    private sealed class NoopStore : IArtifactStore
    {
        public Task<string> SaveAsync(
            string directoryPath,
            string fileNameStem,
            Func<string, string> renderMarkdown,
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
        IServiceCollection services = new ServiceCollection()
            .AddRecallCommanderApplication()
            .AddSingleton<IArtifactStore, NoopStore>()
            .AddSingleton<IArtifactOutputPathProvider, NoopOutputPath>()
            .AddSingleton<IArtifactRenderer<TestArtifact>, TestRenderer>();

        using ServiceProvider provider = services.BuildServiceProvider();

        IArtifactWriter<TestArtifact>? writer = provider.GetService<IArtifactWriter<TestArtifact>>();

        Assert.NotNull(writer);
    }
}
