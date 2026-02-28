using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Services.SystemMetrics.Generator.Schedulers;

namespace TicketFlow.Services.SystemMetrics.Generator;

public static class Extensions
{
    public static IServiceCollection AddSystemMetrics(this IServiceCollection services, IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool>("metrics:system:enabled");
        if (!enabled)
        {
            return services;
        }
        
        services.AddHostedService<RandomMetricsPublisher>();
        return services;
    }
}