using Xunit;
using RecallCommander.Application.Scanning;
using RecallCommander.Application.Tests.Fakes;

namespace RecallCommander.Application.Tests;

public sealed class ScanServiceTests
{
    private readonly InMemoryQuestionSourceRepository _repository = new();
    private readonly FakeFileSystem _fileSystem = new();

    private ScanService CreateService() => new(_repository, _fileSystem, new FakeQuestionBlockParser());

    [Fact]
    public async Task Empty_report_when_no_sources_registered()
    {
        var report = await CreateService().ScanAsync();

        Assert.Equal(0, report.SourceCount);
        Assert.Empty(report.Files);
        Assert.Empty(report.Warnings);
    }

    [Fact]
    public async Task Aggregates_questions_across_files_and_sources()
    {
        await _repository.AddAsync("/notes", DateTimeOffset.UtcNow);
        await _repository.AddAsync("/vault", DateTimeOffset.UtcNow);
        _fileSystem
            .AddDirectory("/notes")
            .AddDirectory("/vault")
            .AddFile("/notes/csharp.md", "question\nquestion")
            .AddFile("/notes/databases.md", "question")
            .AddFile("/vault/physics.md", "question");

        var report = await CreateService().ScanAsync();

        Assert.Equal(2, report.SourceCount);
        Assert.Equal(3, report.Files.Count);
        Assert.Equal(4, report.TotalQuestions);
        Assert.Empty(report.Warnings);
    }

    [Fact]
    public async Task Display_paths_are_relative_to_the_source_directory()
    {
        await _repository.AddAsync("/notes", DateTimeOffset.UtcNow);
        _fileSystem
            .AddDirectory("/notes")
            .AddFile("/notes/csharp/boxing.md", "question");

        var report = await CreateService().ScanAsync();

        var file = Assert.Single(report.Files);
        Assert.Equal("csharp/boxing.md", file.DisplayPath);
        Assert.Equal("/notes/csharp/boxing.md", file.FullPath);
    }

    [Fact]
    public async Task Parser_diagnostics_become_warnings_with_file_and_line()
    {
        await _repository.AddAsync("/notes", DateTimeOffset.UtcNow);
        _fileSystem
            .AddDirectory("/notes")
            .AddFile("/notes/physics.md", "question\nwarning:Missing rc-prompt");

        var report = await CreateService().ScanAsync();

        var warning = Assert.Single(report.Warnings);
        Assert.Equal("physics.md", warning.DisplayPath);
        Assert.Equal(2, warning.LineNumber);
        Assert.Equal("Missing rc-prompt", warning.Message);
        Assert.Equal("physics.md:2", warning.Location);
    }

    [Fact]
    public async Task Missing_source_directory_is_reported_and_scan_continues()
    {
        await _repository.AddAsync("/gone", DateTimeOffset.UtcNow);
        await _repository.AddAsync("/notes", DateTimeOffset.UtcNow);
        _fileSystem
            .AddDirectory("/notes")
            .AddFile("/notes/csharp.md", "question");

        var report = await CreateService().ScanAsync();

        var warning = Assert.Single(report.Warnings);
        Assert.Equal("/gone", warning.DisplayPath);
        Assert.Null(warning.LineNumber);
        Assert.Equal(1, report.TotalQuestions);
    }

    [Fact]
    public async Task Unreadable_file_is_reported_and_scan_continues()
    {
        await _repository.AddAsync("/notes", DateTimeOffset.UtcNow);
        _fileSystem
            .AddDirectory("/notes")
            .AddUnreadableFile("/notes/locked.md")
            .AddFile("/notes/open.md", "question");

        var report = await CreateService().ScanAsync();

        var warning = Assert.Single(report.Warnings);
        Assert.Equal("locked.md", warning.DisplayPath);
        Assert.Contains("Could not read file", warning.Message);
        Assert.Equal(1, report.TotalQuestions);
    }

    [Fact]
    public async Task Overlapping_sources_do_not_scan_the_same_file_twice()
    {
        await _repository.AddAsync("/notes", DateTimeOffset.UtcNow);
        await _repository.AddAsync("/notes/csharp", DateTimeOffset.UtcNow);
        _fileSystem
            .AddDirectory("/notes")
            .AddDirectory("/notes/csharp")
            .AddFile("/notes/csharp/boxing.md", "question");

        var report = await CreateService().ScanAsync();

        Assert.Single(report.Files);
        Assert.Equal(1, report.TotalQuestions);
    }
}
