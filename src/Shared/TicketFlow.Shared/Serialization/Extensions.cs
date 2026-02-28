using Microsoft.Extensions.DependencyInjection;

namespace TicketFlow.Shared.Serialization;

public static class Extensions
{
    public static IServiceCollection AddSerialization(this IServiceCollection services)
    {
        services.AddSingleton<ISerializer, JsonSerializer>();
        return services;
    }
}