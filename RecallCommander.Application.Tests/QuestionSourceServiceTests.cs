using RecallCommander.Application.Sources;
using RecallCommander.Application.Tests.Fakes;
using RecallCommander.Domain;
using Xunit;

namespace RecallCommander.Application.Tests;

public sealed class QuestionSourceServiceTests
{
    private readonly InMemoryQuestionSourceRepository _repository = new();
    private readonly FakeFileSystem _fileSystem = new();

    private QuestionSourceService CreateService() =>
        new(_repository, _fileSystem, TimeProvider.System);

    [Fact]
    public async Task Adds_existing_directory_as_source()
    {
        _fileSystem.AddDirectory("/notes");

        AddSourceResult result = await CreateService().AddAsync("/notes");

        Assert.Equal(AddSourceStatus.Added, result.Status);
        Assert.NotNull(result.Source);
        Assert.Equal("/notes", result.Source.DirectoryPath);
        Assert.Single(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task Rejects_missing_directory()
    {
        AddSourceResult result = await CreateService().AddAsync("/does-not-exist");

        Assert.Equal(AddSourceStatus.DirectoryNotFound, result.Status);
        Assert.Empty(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task Reports_already_registered_directory()
    {
        _fileSystem.AddDirectory("/notes");
        QuestionSourceService service = CreateService();
        await service.AddAsync("/notes");

        AddSourceResult result = await service.AddAsync("/notes");

        Assert.Equal(AddSourceStatus.AlreadyRegistered, result.Status);
        Assert.Single(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task Removes_a_registered_source()
    {
        _fileSystem.AddDirectory("/notes");
        QuestionSourceService service = CreateService();
        await service.AddAsync("/notes");

        RemoveSourceResult result = await service.RemoveAsync("/notes");

        Assert.Equal(RemoveSourceStatus.Removed, result.Status);
        Assert.Equal("/notes", result.DirectoryPath);
        Assert.Empty(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task Removing_an_unregistered_source_is_reported()
    {
        RemoveSourceResult result = await CreateService().RemoveAsync("/never-added");

        Assert.Equal(RemoveSourceStatus.NotRegistered, result.Status);
    }

    [Fact]
    public async Task Removes_a_stale_source_whose_directory_no_longer_exists()
    {
        _fileSystem.AddDirectory("/notes");
        QuestionSourceService service = CreateService();
        await service.AddAsync("/notes");

        // Simulate the directory disappearing after registration.
        RemoveSourceResult result = await new QuestionSourceService(
            _repository, new FakeFileSystem(), TimeProvider.System).RemoveAsync("/notes");

        Assert.Equal(RemoveSourceStatus.Removed, result.Status);
        Assert.Empty(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task Lists_sources_in_registration_order()
    {
        _fileSystem.AddDirectory("/a").AddDirectory("/b");
        QuestionSourceService service = CreateService();
        await service.AddAsync("/a");
        await service.AddAsync("/b");

        IReadOnlyList<QuestionSource> sources = await service.ListAsync();

        Assert.Equal(["/a", "/b"], sources.Select(source => source.DirectoryPath));
    }
}
