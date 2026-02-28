using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using TicketFlow.Agents.Shared.Configuration;
using TicketFlow.Agents.Shared.Langfuse;
using TicketFlow.Agents.Shared.Metrics;
using TicketFlow.Agents.Shared.Services;

namespace TicketFlow.Agents.Shared;

public static class LlmExtensions
{
    public static IServiceCollection AddLlmClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Llm")
    {
        services.Configure<LlmOptions>(configuration.GetSection(sectionName));
        var llmOptions = configuration.GetSection(sectionName).Get<LlmOptions>() ?? new LlmOptions();
        var agentName = configuration["App:AppName"] ?? "unknown-agent";
        var metricsEnabled = configuration.GetValue<bool>("metrics:prometheus:enabled");
        
        services.AddSingleton<IChatClient>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<QwenToolCallingChatClient>>();
            IChatClient baseChatClient;
            string modelName;

            if (llmOptions.Provider.Equals("openai", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(llmOptions.OpenAIApiKey))
                {
                    throw new InvalidOperationException(
                        $"OpenAI API Key is required when Provider is set to 'openai'. " +
                        $"Set {sectionName}:OpenAIApiKey in configuration.");
                }

                modelName = llmOptions.OpenAIModel;
                var openAiClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential(llmOptions.OpenAIApiKey));
                baseChatClient = openAiClient.GetChatClient(modelName).AsIChatClient();
            }
            else if (llmOptions.Provider.Equals("openrouter", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(llmOptions.OpenRouterApiKey))
                {
                    throw new InvalidOperationException(
                        $"OpenRouter API Key is required when Provider is set to 'openrouter'. " +
                        $"Set {sectionName}:OpenRouterApiKey in configuration. " +
                        $"Get free key at https://openrouter.ai/");
                }

                modelName = llmOptions.OpenRouterModel;
                // OpenRouter is OpenAI-compatible - just change the endpoint
                var openRouterClient = new OpenAIClient(
                    new System.ClientModel.ApiKeyCredential(llmOptions.OpenRouterApiKey),
                    new OpenAIClientOptions { Endpoint = new Uri("https://openrouter.ai/api/v1") });
                baseChatClient = openRouterClient.GetChatClient(modelName).AsIChatClient();
            }
            else
            {
                // Default to Ollama (local)
                modelName = llmOptions.OllamaModel;
                var openAiClient = new OpenAIClient(
                    new System.ClientModel.ApiKeyCredential("ollama"),
                    new OpenAIClientOptions { Endpoint = new Uri($"{llmOptions.OllamaBaseUrl}/v1") });
                var ollamaChatClient = openAiClient.GetChatClient(modelName).AsIChatClient();

                if (modelName.Contains("qwen", StringComparison.OrdinalIgnoreCase))
                {
                    baseChatClient = new QwenToolCallingChatClient(ollamaChatClient, logger);
                }
                else
                {
                    baseChatClient = ollamaChatClient;
                }
            }

            if (metricsEnabled)
            {
                var metrics = sp.GetService<AiMetrics>();
                if (metrics != null)
                {
                    baseChatClient = new InstrumentedChatClient(baseChatClient, metrics, modelName, agentName);
                }
            }

            if (!llmOptions.DisableLangfuseChatClientWrapper)
            {
                var langfuse = sp.GetService<ILangfuseClient>();
                if (langfuse?.IsEnabled == true)
                {
                    var tracingLogger = sp.GetService<ILogger<LangfuseTracingChatClient>>();
                    baseChatClient = new LangfuseTracingChatClient(baseChatClient, langfuse, agentName, modelName, tracingLogger);
                }
            }

            return baseChatClient;
        });

        return services;
    }
}
