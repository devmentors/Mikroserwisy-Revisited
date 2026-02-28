using Microsoft.Extensions.DependencyInjection;

namespace TicketFlow.Services.Tickets.Core.Initializers;

public static class Extensions
{
    public static IServiceCollection AddAppInitializers(this IServiceCollection services)
    {
        services.AddHostedService<AgentsAppInitializer>();
        return services;
    }
}