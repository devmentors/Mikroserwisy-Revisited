using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TicketFlow.Shared.Ollama;

public static class Extensions
{
    private const string SectionName = "Ollama";

    public static IServiceCollection AddOllama(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = SectionName)
    {
        services.Configure<OllamaOptions>(configuration.GetSection(sectionName));

        services.AddHttpClient<OllamaChatClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OllamaOptions>>();
            client.BaseAddress = new Uri(options.Value.BaseUrl);
            client.Timeout = TimeSpan.FromMinutes(5); // Ollama może być wolne na CPU
        });

        services.AddSingleton<IChatClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(OllamaChatClient));
            var options = sp.GetRequiredService<IOptions<OllamaOptions>>();
            var logger = sp.GetRequiredService<ILogger<OllamaChatClient>>();

            return new OllamaChatClient(httpClient, options, logger);
        });

        return services;
    }

    public static IServiceCollection AddOllamaWithTracing(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = SectionName)
    {
        services.AddOllama(configuration, sectionName);

        // Add traced wrapper
        services.Decorate<IChatClient>((inner, sp) =>
        {
            var logger = sp.GetRequiredService<ILogger<TracedChatClient>>();
            return new TracedChatClient(inner, logger);
        });

        return services;
    }
}
