using Microsoft.Extensions.DependencyInjection;
using RecallCommander.Application.Abstractions;
using RecallCommander.Infrastructure.Database;
using RecallCommander.Infrastructure.FileSystem;

namespace RecallCommander.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRecallCommanderInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDataPaths, DataPaths>();
        services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
        services.AddSingleton<IWorkspaceInitializer, WorkspaceInitializer>();
        services.AddSingleton<IQuestionSourceRepository, SqliteQuestionSourceRepository>();
        services.AddSingleton<IFileSystem, PhysicalFileSystem>();
        return services;
    }
}
