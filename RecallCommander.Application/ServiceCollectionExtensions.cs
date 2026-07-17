using Microsoft.Extensions.DependencyInjection;
using RecallCommander.Application.Artifacts;
using RecallCommander.Application.Assessments;
using RecallCommander.Application.Attempts;
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
        services.AddSingleton<IQuestionSelector, RandomQuestionSelector>();
        services.AddSingleton<CreateAssessmentService>();
        services.AddSingleton<ValidateAttemptService>();
        services.AddSingleton<ArtifactFileNameGenerator>();

        // Open generic: IArtifactWriter<T> resolves for any T that has a
        // registered IArtifactRenderer<T>.
        services.AddSingleton(typeof(IArtifactWriter<>), typeof(ArtifactWriter<>));

        return services;
    }
}
