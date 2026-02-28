using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketFlow.Shared.Caching;
using TicketFlow.Shared.OpenAI;

namespace TicketFlow.Services.Communication.Core.Translations;

internal static class Extensions
{
    public static IServiceCollection AddLocalTranslations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOpenAi(configuration);
        services.AddCachingWithFallback(configuration);

        services.AddSingleton<ILocalTranslationsService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAIOptions>>();

            if (!options.Value.Enabled)
            {
                return new NoopLocalTranslationsService();
            }

            var chatClient = sp.GetService<IChatClient>();
            var cacheService = sp.GetRequiredService<ICacheService>();
            var logger = sp.GetRequiredService<ILogger<LocalTranslationsService>>();

            return new LocalTranslationsService(chatClient, cacheService, logger);
        });

        return services;
    }
}
