using Microsoft.Extensions.DependencyInjection;
using RecallCommander.Application.Abstractions;
using RecallCommander.Markdown.Parsing;

namespace RecallCommander.Markdown;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRecallCommanderMarkdown(this IServiceCollection services)
    {
        services.AddSingleton<IQuestionBlockParser, QuestionBlockParser>();
        return services;
    }
}
