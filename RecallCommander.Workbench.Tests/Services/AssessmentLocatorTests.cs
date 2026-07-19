using RecallCommander.Markdown.Writing;
using RecallCommander.Workbench.Services;
using RecallCommander.Workbench.Tests.Support;
using Xunit;

namespace RecallCommander.Workbench.Tests.Services;

public sealed class AssessmentLocatorTests
{
    private readonly FakeFileSystem _fileSystem = new();

    private AssessmentLocator CreateLocator() => new(
        new FixedArtifactOutputPathProvider("/output"),
        new AssessmentRenderer(),
        _fileSystem);

    [Fact]
    public void Lists_assessment_files_newest_first()
    {
        _fileSystem
            .AddDirectory("/output/Assessments")
            .AddFile("/output/Assessments/assessment-2026-07-18-001.md", "old")
            .AddFile("/output/Assessments/assessment-2026-07-19-001.md", "newer")
            .AddFile("/output/Assessments/assessment-2026-07-19-002.md", "newest");

        IReadOnlyList<ArtifactFile> files = CreateLocator().List();

        Assert.Equal(
            [
                "assessment-2026-07-19-002.md",
                "assessment-2026-07-19-001.md",
                "assessment-2026-07-18-001.md",
            ],
            files.Select(file => file.FileName));
        Assert.Equal("/output/Assessments/assessment-2026-07-19-002.md", files[0].FilePath);
    }

    [Fact]
    public void A_missing_output_directory_yields_an_empty_list()
    {
        Assert.Empty(CreateLocator().List());
    }
}
