using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Agents.Shared.A2A;
using TicketFlow.Agents.Shared.Configuration;
using TicketFlow.Agents.Shared.Langfuse;
using TicketFlow.Agents.Shared.Services;

namespace TicketFlow.Agents.Shared;

public static class Extensions
{

    public static IServiceCollection AddAgentSharedServices(this IServiceCollection services)
    {
        services.AddSingleton<IUserContextExtractor, UserContextExtractor>();
        services.AddSingleton<ISseWriter, SseWriter>();
        services.AddSingleton<IAguiRequestHandler, AguiRequestHandler>();
        services.AddSingleton<IMcpClientFactory, McpClientFactory>();
        return services;
    }
    
    public static IServiceCollection AddLangfuse(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LangfuseOptions>(configuration.GetSection(LangfuseOptions.SectionName));
        services.AddHttpClient<ILangfuseClient, LangfuseClient>();
        return services;
    }
}
