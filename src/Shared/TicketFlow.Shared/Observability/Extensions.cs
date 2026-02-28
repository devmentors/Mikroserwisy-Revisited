using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TicketFlow.Shared.Observability;

public static class Extensions
{
    private const string ObservabilitySection = "observability";

    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration, string sectionName = ObservabilitySection)
    {
        var serviceName = configuration.GetValue<string>("App:AppName");
        var enabled = configuration.GetValue<bool>($"{sectionName}:enabled");

        if (enabled is false)
        {
            return services;
        }
        
        var section = configuration.GetSection(sectionName);
        services.Configure<ObservabilityOptions>(section);
        var endpoint = configuration.GetValue<string>($"{sectionName}:endpoint");

        services.AddOpenTelemetry().WithTracing(x =>
        {
            x
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                   .AddService(serviceName))
            .AddSource(
                MessagingActivitySources.MessagingPublishSourceName, 
                MessagingActivitySources.MessagingConsumeSourceName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(endpoint);
            });
        });
        
        return services;
    }
}