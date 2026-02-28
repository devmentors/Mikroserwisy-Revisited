using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Services.Communication.Core.Data;
using TicketFlow.Services.Communication.Core.Http.Tickets;
using TicketFlow.Services.Communication.Core.Messaging;
using TicketFlow.Services.SystemMetrics.Generator;
using TicketFlow.Shared.AnomalyGeneration;
using TicketFlow.Shared.App;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Data;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Deduplication;
using TicketFlow.Shared.Messaging.Outbox;
using TicketFlow.Shared.Messaging.RabbitMQ;
using TicketFlow.Shared.Messaging.Resiliency;
using TicketFlow.Shared.Messaging.Topology;
using TicketFlow.Shared.Metrics;
using TicketFlow.Shared.Observability;
using TicketFlow.Shared.Queries;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.Services.Communication.Core;

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
                .UseMessageConsumerConvention<DontUseConventionalTopology>()
                .UseDeduplication()
                .UseOutbox()
                .UseAnomalies()
                .UseResiliency())
            .AddPostgres<CommunicationDbContext>(configuration)
            .AddSystemMetrics(configuration)
            .AddMetrics(configuration)
            .AddObservability(configuration);


        services.AddHttpClient<ITicketsClient, TicketsClient>(builder =>
        {
            builder.BaseAddress = new Uri(configuration.GetValue<string>("Services:Tickets"));
        });

        services.AddHostedService<CommunicationConsumer>();
        services.AddHostedService<CommunicationTopologyInitializer>();
        
        return services;
    }
}