using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Agents.Shared.A2A;
using TicketFlow.Agents.Shared.Services;

namespace TicketFlow.Agents.Shared;

public static class A2AExtensions
{
    public static IServiceCollection AddA2ASupport(this IServiceCollection services)
    {
        services.AddHttpClient<A2AAgentCardClient>();
        services.AddHttpClient<DynamicA2AToolsBuilder>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(3);
        });

        services.AddTransient<A2AContextPropagationHandler>();
        services.AddHttpClient<SdkA2AToolsBuilder>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(3);
        }).AddHttpMessageHandler<A2AContextPropagationHandler>();

        return services;
    }
}
