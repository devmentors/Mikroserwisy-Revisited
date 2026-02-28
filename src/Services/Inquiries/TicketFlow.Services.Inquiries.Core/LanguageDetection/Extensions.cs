using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TicketFlow.Shared.OpenAI;

namespace TicketFlow.Services.Inquiries.Core.LanguageDetection;

public static class Extensions
{
    public static IServiceCollection AddLanguageDetection(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenAi(configuration);
        services.AddSingleton<ILanguageDetector>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAIOptions>>();

            return options.Value.Enabled
                ? new OpenAiLanguageDetector(sp.GetRequiredService<IChatClient>())
                : new NoopLanguageDetector();
        });

        return services;
    }
}