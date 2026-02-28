using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TicketFlow.Shared.OpenAI;

namespace TicketFlow.Services.Translations.Core.Translations;

internal static class Extensions
{
    public static IServiceCollection AddTranslations(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenAi(configuration);
        services.AddSingleton<ITranslationsService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAIOptions>>();

            return options.Value.Enabled
                ? new OpenAiTranslationsService(sp.GetRequiredService<IChatClient>())
                : new NoopTranslationsService();
        });
        return services;
    }
}