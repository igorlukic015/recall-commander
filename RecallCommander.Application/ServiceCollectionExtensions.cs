using Microsoft.Extensions.DependencyInjection;
using RecallCommander.Application.Scanning;
using RecallCommander.Application.Sources;

namespace RecallCommander.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRecallCommanderApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<QuestionSourceService>();
        services.AddSingleton<ScanService>();
        return services;
    }
}
