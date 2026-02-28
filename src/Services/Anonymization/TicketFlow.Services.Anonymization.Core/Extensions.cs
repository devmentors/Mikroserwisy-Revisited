using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Services.Anonymization.Core.Data;
using TicketFlow.Services.Anonymization.Core.Http;
using TicketFlow.Services.Anonymization.Core.Messaging;
using TicketFlow.Services.Anonymization.Core.Messaging.Consuming.Handlers;
using TicketFlow.Services.Anonymization.Core.Repositories;
using TicketFlow.Shared.App;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Data;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.RabbitMQ;
using TicketFlow.Shared.Messaging.Resiliency;
using TicketFlow.Shared.Messaging.Topology;
using TicketFlow.Shared.AnomalyGeneration;
using TicketFlow.Shared.Queries;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.Services.Anonymization.Core;

public static class Extensions
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddExceptions()
            .AddApp(configuration)
            .AddSerialization(configuration)
            .AddCommands()
            .AddQueries()
            .AddMessaging(configuration, x => x
                .UseRabbitMq()
                .UseMessageConsumerConvention<DontUseConventionalTopology>()
                .UseResiliency()
                .UseAnomalies())
            .AddPostgres<AnonymizationDbContext>(configuration);

        var vaultUrl = configuration["Services:PersonalInfoVault"] ?? "http://localhost:6100";
        services.AddHttpClient<IPersonalInfoVaultClient, PersonalInfoVaultClient>(client =>
        {
            client.BaseAddress = new Uri(vaultUrl);
        });

        services.AddScoped<IAnonymizationRepository, AnonymizationRepository>();
        services.AddScoped<AnonymizationCompletedHandler>();

        services.AddHostedService<AnonymizationTopologyInitializer>();
        services.AddHostedService<AnonymizationConsumerService>();

        return services;
    }
}
