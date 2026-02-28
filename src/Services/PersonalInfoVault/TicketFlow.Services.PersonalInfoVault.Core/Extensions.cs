using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Services.PersonalInfoVault.Core.Data;
using TicketFlow.Services.PersonalInfoVault.Core.Initializers;
using TicketFlow.Services.PersonalInfoVault.Core.Messaging;
using TicketFlow.Services.PersonalInfoVault.Core.Repositories;
using TicketFlow.Services.PersonalInfoVault.Core.Services;
using TicketFlow.Shared.AnomalyGeneration;
using TicketFlow.Shared.App;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Data;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.RabbitMQ;
using TicketFlow.Shared.Messaging.Resiliency;
using TicketFlow.Shared.Messaging.Topology;
using TicketFlow.Shared.Metrics;
using TicketFlow.Shared.Queries;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.Services.PersonalInfoVault.Core;

public static class Extensions
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPersonalInfoRepository, PersonalInfoRepository>();
        services.AddSingleton<ITokenGenerator, TokenGenerator>();

        services
            .AddExceptions()
            .AddSerialization(configuration)
            .AddApp(configuration)
            .AddCommands()
            .AddQueries()
            .AddPostgres<PersonalInfoVaultDbContext>(configuration)
            .AddMessaging(configuration, x => x
                .UseRabbitMq()
                .UseMessageConsumerConvention<DontUseConventionalTopology>()
                .UseAnomalies()
                .UseResiliency())
            .AddMetrics(configuration);

        services.AddHostedService<DbInitializer>();
        services.AddHostedService<VaultTopologyInitializer>();
        services.AddHostedService<VaultConsumerService>();

        return services;
    }
}
