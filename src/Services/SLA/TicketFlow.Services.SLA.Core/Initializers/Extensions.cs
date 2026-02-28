using Microsoft.Extensions.DependencyInjection;

namespace TicketFlow.Services.SLA.Core.Initializers;

public static class Extensions
{
    public static IServiceCollection AddAppInitializers(this IServiceCollection services)
    {
        services.AddHostedService<SLASignedAppInitializer>();
        return services;
    }
}