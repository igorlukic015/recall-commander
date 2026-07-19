using RecallCommander.Contracts.Artifacts;
using RecallCommander.Infrastructure.Artifacts;
using RecallCommander.IntegrationTests.Support;
using RecallCommander.Markdown.Writing;
using Xunit;

namespace RecallCommander.IntegrationTests.Integration;

/// <summary>
/// Markdown builder output persisted through the real artifact store to the
/// real filesystem.
/// </summary>
public sealed class ArtifactStoreTests : IDisposable
{
    private readonly TestWorkspace _workspace = new();
    private readonly ArtifactFileStore _store = new(new ArtifactFileNameGenerator());

    public void Dispose() => _workspace.Dispose();

    [Fact]
    public async Task Creates_the_output_directory_and_writes_the_document()
    {
        string directory = Path.Combine(_workspace.Root, "Assessments");
        string document = new MarkdownArtifactBuilder()
            .WithFrontmatter(new { Type = "assessment", Title = "Test" })
            .AppendHeading(1, "Test")
            .Build();

        string path = await _store.SaveAsync(directory, "assessment-2026-07-16", _ => document);

        Assert.True(Directory.Exists(directory));
        Assert.Equal(document, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Assigns_deterministic_sequence_numbers()
    {
        string directory = Path.Combine(_workspace.Root, "Assessments");

        string first = await _store.SaveAsync(directory, "assessment-2026-07-16", _ => "one");
        string second = await _store.SaveAsync(directory, "assessment-2026-07-16", _ => "two");
        string third = await _store.SaveAsync(directory, "assessment-2026-07-16", _ => "three");

        Assert.Equal("assessment-2026-07-16-001.md", Path.GetFileName(first));
        Assert.Equal("assessment-2026-07-16-002.md", Path.GetFileName(second));
        Assert.Equal("assessment-2026-07-16-003.md", Path.GetFileName(third));
        Assert.Equal("one", await File.ReadAllTextAsync(first));
        Assert.Equal("three", await File.ReadAllTextAsync(third));
    }

    [Fact]
    public async Task Different_stems_get_independent_sequences()
    {
        string directory = Path.Combine(_workspace.Root, "Assessments");

        await _store.SaveAsync(directory, "assessment-2026-07-16", _ => "a");
        string otherDay = await _store.SaveAsync(directory, "assessment-2026-07-17", _ => "b");

        Assert.Equal("assessment-2026-07-17-001.md", Path.GetFileName(otherDay));
    }

    [Fact]
    public async Task Rendering_receives_the_artifact_id_matching_the_file_name()
    {
        string directory = Path.Combine(_workspace.Root, "Assessments");

        string first = await _store.SaveAsync(directory, "assessment-2026-07-16", id => $"id: {id}");
        string second = await _store.SaveAsync(directory, "assessment-2026-07-16", id => $"id: {id}");

        Assert.Equal("id: assessment-2026-07-16-001", await File.ReadAllTextAsync(first));
        Assert.Equal("id: assessment-2026-07-16-002", await File.ReadAllTextAsync(second));
        Assert.Equal(
            Path.GetFileNameWithoutExtension(second),
            (await File.ReadAllTextAsync(second))["id: ".Length..]);
    }
}
