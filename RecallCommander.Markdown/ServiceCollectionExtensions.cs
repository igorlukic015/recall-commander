using Microsoft.Extensions.DependencyInjection;
using RecallCommander.Contracts.Artifacts;
using RecallCommander.Contracts.Attempts;
using RecallCommander.Contracts.Questions;
using RecallCommander.Domain;
using RecallCommander.Markdown.Parsing;
using RecallCommander.Markdown.Writing;

namespace RecallCommander.Markdown;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRecallCommanderMarkdown(this IServiceCollection services)
    {
        services.AddSingleton<IQuestionBlockParser, QuestionBlockParser>();
        services.AddSingleton<IAttemptParser, AttemptParser>();
        services.AddSingleton<IArtifactRenderer<Assessment>, AssessmentRenderer>();
        return services;
    }
}
