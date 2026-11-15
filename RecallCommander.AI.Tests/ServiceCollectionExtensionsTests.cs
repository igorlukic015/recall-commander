using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecallCommander.AI.Evaluation;
using RecallCommander.Application;
using RecallCommander.Application.Reviews;
using RecallCommander.Contracts.Reviews;
using Xunit;

namespace RecallCommander.AI.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    private static IQuestionEvaluator ResolveEvaluator(params (string Key, string Value)[] settings)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(
                setting => setting.Key,
                string? (setting) => setting.Value))
            .Build();

        ServiceCollection services = new ServiceCollection();
        services.AddRecallCommanderApplication();
        services.AddRecallCommanderAi(configuration);

        return services.BuildServiceProvider().GetRequiredService<IQuestionEvaluator>();
    }

    [Fact]
    public void Without_configuration_the_fake_evaluator_stays_active()
    {
        Assert.IsType<FakeQuestionEvaluator>(ResolveEvaluator());
    }

    [Fact]
    public void The_fake_provider_can_be_selected_explicitly()
    {
        Assert.IsType<FakeQuestionEvaluator>(ResolveEvaluator(("Ai:Provider", "fake")));
    }

    [Fact]
    public void The_ollama_provider_activates_the_ai_evaluator()
    {
        IQuestionEvaluator evaluator = ResolveEvaluator(
            ("Ai:Provider", "ollama"),
            ("Ai:Ollama:Model", "llama3.2"));

        Assert.IsType<AiQuestionEvaluator>(evaluator);
        Assert.Equal("ollama/llama3.2", evaluator.Name);
    }

    [Fact]
    public void The_provider_name_is_case_insensitive()
    {
        IQuestionEvaluator evaluator = ResolveEvaluator(
            ("Ai:Provider", "Ollama"),
            ("Ai:Ollama:Model", "llama3.2"));

        Assert.IsType<AiQuestionEvaluator>(evaluator);
    }

    [Fact]
    public void The_gemini_provider_activates_the_ai_evaluator()
    {
        IQuestionEvaluator evaluator = ResolveEvaluator(
            ("Ai:Provider", "gemini"),
            ("Ai:Gemini:Model", "gemini-2.0-flash"),
            ("Ai:Gemini:ApiKey", "test-key"));

        Assert.IsType<AiQuestionEvaluator>(evaluator);
        Assert.Equal("gemini/gemini-2.0-flash", evaluator.Name);
    }

    [Fact]
    public void Gemini_without_an_api_key_fails_on_resolution_with_a_clear_message()
    {
        AiException exception = Assert.Throws<AiException>(() => ResolveEvaluator(
            ("Ai:Provider", "gemini"),
            ("Ai:Gemini:Model", "gemini-2.0-flash")));

        Assert.Contains("API key", exception.Message);
    }

    [Fact]
    public void Ollama_without_a_model_fails_on_resolution_with_a_clear_message()
    {
        AiException exception = Assert.Throws<AiException>(() =>
            ResolveEvaluator(("Ai:Provider", "ollama")));

        Assert.Contains("Ai:Ollama:Model", exception.Message);
    }

    [Fact]
    public void An_unknown_provider_fails_on_resolution()
    {
        AiException exception = Assert.Throws<AiException>(() =>
            ResolveEvaluator(("Ai:Provider", "chatgpt")));

        Assert.Contains("chatgpt", exception.Message);
    }
}
