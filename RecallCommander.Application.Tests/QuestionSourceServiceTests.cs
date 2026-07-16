using Xunit;
using RecallCommander.Application.Sources;
using RecallCommander.Application.Tests.Fakes;

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

        var result = await CreateService().AddAsync("/notes");

        Assert.Equal(AddSourceStatus.Added, result.Status);
        Assert.NotNull(result.Source);
        Assert.Equal("/notes", result.Source.DirectoryPath);
        Assert.Single(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task Rejects_missing_directory()
    {
        var result = await CreateService().AddAsync("/does-not-exist");

        Assert.Equal(AddSourceStatus.DirectoryNotFound, result.Status);
        Assert.Empty(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task Reports_already_registered_directory()
    {
        _fileSystem.AddDirectory("/notes");
        var service = CreateService();
        await service.AddAsync("/notes");

        var result = await service.AddAsync("/notes");

        Assert.Equal(AddSourceStatus.AlreadyRegistered, result.Status);
        Assert.Single(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task Lists_sources_in_registration_order()
    {
        _fileSystem.AddDirectory("/a").AddDirectory("/b");
        var service = CreateService();
        await service.AddAsync("/a");
        await service.AddAsync("/b");

        var sources = await service.ListAsync();

        Assert.Equal(["/a", "/b"], sources.Select(source => source.DirectoryPath));
    }
}
