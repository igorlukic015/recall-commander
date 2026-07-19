using Microsoft.Extensions.DependencyInjection;
using RecallCommander.Application.Artifacts;
using RecallCommander.Application.Assessments;
using RecallCommander.Application.Attempts;
using RecallCommander.Application.Reviews;
using RecallCommander.Application.Scanning;
using RecallCommander.Application.Sources;
using RecallCommander.Contracts.Artifacts;
using RecallCommander.Contracts.Assessments;
using RecallCommander.Contracts.Reviews;

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
        services.AddSingleton<IQuestionEvaluator, FakeQuestionEvaluator>();
        services.AddSingleton<CreateReviewService>();
        services.AddSingleton<ArtifactFileNameGenerator>();

        // Open generic: IArtifactWriter<T> resolves for any T that has a
        // registered IArtifactRenderer<T>.
        services.AddSingleton(typeof(IArtifactWriter<>), typeof(ArtifactWriter<>));

        return services;
    }
}
