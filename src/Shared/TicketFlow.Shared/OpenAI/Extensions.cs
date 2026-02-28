using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;

namespace TicketFlow.Shared.OpenAI;

public static class Extensions
{
    private const string SectionName = "OpenAI";
    
    public static IServiceCollection AddOpenAi(this IServiceCollection services, IConfiguration configuration, string sectionName = SectionName)
    {
        services.Configure<OpenAIOptions>(configuration.GetSection(sectionName));
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAIOptions>>();
            return new OpenAIClient(options.Value.ApiKey).AsChatClient(modelId: "gpt-4o-mini");
        });
        
        return services;
    }
}