using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace TicketFlow.Shared.Metrics;

public static class Extensions
{
    public static IServiceCollection AddMetrics(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration.GetValue<string>("App:AppName");
        var enabled = configuration.GetValue<bool>("metrics:prometheus:enabled");

        if (!enabled)
        {
            return services;
        }

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter();
            });

        return services;
    }

    public static WebApplication UseMetrics(this WebApplication app)
    {
        var enabled = app.Configuration.GetValue<bool>("metrics:prometheus:enabled");

        if (!enabled)
        {
            return app;
        }

        app.MapPrometheusScrapingEndpoint();

        return app;
    }
}
