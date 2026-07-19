using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecallCommander.AI.Clients;
using RecallCommander.AI.Configuration;
using RecallCommander.AI.Evaluation;
using RecallCommander.AI.Prompts;
using RecallCommander.Contracts.Reviews;

namespace RecallCommander.AI;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Wires the AI evaluation boundary from the "Ai" configuration section.
    /// With the default provider ("fake") nothing is registered and the
    /// deterministic evaluator from the Application module stays active, so
    /// no command ever reaches the network unless a real provider is
    /// configured. Configuration mistakes surface when the evaluator is
    /// resolved, not here — commands that don't use AI keep working.
    /// </summary>
    public static IServiceCollection AddRecallCommanderAi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AiOptions options = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
        string provider = string.IsNullOrWhiteSpace(options.Provider)
            ? AiOptions.FakeProvider
            : options.Provider.Trim().ToLowerInvariant();

        switch (provider)
        {
            case AiOptions.FakeProvider:
                return services;

            case AiOptions.OllamaProvider:
                services.AddSingleton(options.Ollama);
                services.AddHttpClient<IAiClient, OllamaAiClient>();
                return AddAiEvaluation(services);

            case AiOptions.GeminiProvider:
                services.AddSingleton(options.Gemini);
                services.AddHttpClient<IAiClient, GeminiAiClient>();
                return AddAiEvaluation(services);

            default:
                services.AddSingleton<IQuestionEvaluator>(_ => throw new AiException(
                    $"Unknown AI provider '{options.Provider}'. Expected 'fake', 'ollama' or 'gemini'."));
                return services;
        }
    }

    private static IServiceCollection AddAiEvaluation(IServiceCollection services)
    {
        services.AddSingleton<PromptLoader>();
        services.AddSingleton<ReviewPromptBuilder>();
        services.AddSingleton<EvaluationResponseParser>();

        // Registered after the Application module's fake evaluator, so this
        // registration wins when the container resolves IQuestionEvaluator.
        services.AddSingleton<IQuestionEvaluator, AiQuestionEvaluator>();
        return services;
    }
}
