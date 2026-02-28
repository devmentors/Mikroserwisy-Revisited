using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Services.Aggregation.Core.Data;
using TicketFlow.Services.Aggregation.Core.Http;
using TicketFlow.Services.Aggregation.Core.Messaging;
using TicketFlow.Services.Aggregation.Core.Messaging.Consuming.Handlers;
using TicketFlow.Services.Aggregation.Core.Repositories;
using TicketFlow.Shared.App;
using TicketFlow.Shared.Data;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.RabbitMQ;
using TicketFlow.Shared.Messaging.Resiliency;
using TicketFlow.Shared.Messaging.Topology;
using TicketFlow.Shared.AnomalyGeneration;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.Services.Aggregation.Core;

public static class Extensions
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddExceptions()
            .AddApp(configuration)
            .AddSerialization()
            .AddMessaging(configuration, x => x
                .UseRabbitMq()
                .UseMessageConsumerConvention<DontUseConventionalTopology>()
                .UseResiliency()
                .UseAnomalies())
            .AddPostgres<AggregationDbContext>(configuration);

        services.AddHttpClient<ITicketsClient, TicketsClient>(client =>
        {
            client.BaseAddress = new Uri(configuration.GetValue<string>("Services:Tickets"));
        });

        var vaultUrl = configuration["Services:PersonalInfoVault"] ?? "http://localhost:6100";
        services.AddHttpClient<IPersonalInfoVaultClient, PersonalInfoVaultClient>(client =>
        {
            client.BaseAddress = new Uri(vaultUrl);
        });

        services.AddScoped<ITicketProjectionRepository, TicketProjectionRepository>();

        services.AddScoped<TicketCreatedHandler>();
        services.AddScoped<TicketQualifiedHandler>();
        services.AddScoped<TicketResolvedHandler>();
        services.AddScoped<AgentAssignedHandler>();
        services.AddScoped<DeadlinesCalculatedHandler>();
        services.AddScoped<SLABreachedHandler>();
        services.AddScoped<AnonymizationRequestedHandler>();

        services.AddHostedService<AggregationTopologyInitializer>();
        services.AddHostedService<AggregationConsumerService>();

        return services;
    }
}
