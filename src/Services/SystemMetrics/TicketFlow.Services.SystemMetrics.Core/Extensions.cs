using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Services.SystemMetrics.Core.LiveMetrics;
using TicketFlow.Shared.AnomalyGeneration;
using TicketFlow.Shared.App;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.RabbitMQ;
using TicketFlow.Shared.Messaging.Resiliency;
using TicketFlow.Shared.Metrics;
using TicketFlow.Shared.Queries;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.Services.SystemMetrics.Core;

public static class Extensions
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddExceptions()
            .AddSerialization()
            .AddApp(configuration)
            .AddCommands()
            .AddQueries()
            .AddLogging()
            .AddMessaging(configuration, x => x
                .UseRabbitMq()
                .UseAnomalies()
                .UseResiliency())
            .AddMetrics(configuration);

        services.AddSignalR()
            .AddJsonProtocol(opt => opt.PayloadSerializerOptions = new JsonSerializerOptions(SerializationOptions.Default));
        
        services.AddSingleton(new LiveMetricsOptions
        {
            PollingIntervalInMs = configuration.GetValue<int?>("LiveMetrics:PollingIntervalInMs") ?? 100
        });
        services.AddSingleton<LiveMetricsHub>();
        services.AddHostedService<LiveMetricsPullService>();
        
        return services;
    }
    
    public static void ExposeLiveMetrics(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<LiveMetricsHub>("/live-metrics");
    }
}