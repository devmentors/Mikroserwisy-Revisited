using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Services.Inquiries.Core.Data;
using TicketFlow.Services.Inquiries.Core.Data.Repositories;
using TicketFlow.Services.Inquiries.Core.Initializers;
using TicketFlow.Services.Inquiries.Core.Http;
using TicketFlow.Services.Inquiries.Core.LanguageDetection;
using TicketFlow.Services.Inquiries.Core.Messaging;
using TicketFlow.Services.SystemMetrics.Generator;
using TicketFlow.Shared.AnomalyGeneration;
using TicketFlow.Shared.App;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Data;
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
using TicketFlow.Services.Inquiries.Core.Clients;

namespace TicketFlow.Services.Inquiries.Core;

public static class Extensions
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IInquiriesRepository, InquiriesRepository>();

        services
            .AddServiceClients(configuration)
            .AddSerialization()
            .AddApp(configuration)
            .AddCommands()
            .AddQueries()
            .AddLogging()
            .AddMessaging(configuration, x => x
                .UseRabbitMq()
                .UseMessageConsumerConvention<DontUseConventionalTopology>()
                .UseAnomalies()
                .UseResiliency())
            .AddPostgres<InquiriesDbContext>(configuration)
            .AddLanguageDetection(configuration)
            .AddSystemMetrics(configuration)
            .AddMetrics(configuration)
            .AddObservability(configuration);

        services.AddHostedService<DbInitializer>();
        services.AddHostedService<InquiriesConsumerService>();
        services.AddHostedService<InquiriesTopologyInitializer>();

        return services;
    }

    private static IServiceCollection AddServiceClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<ITicketsClient, TicketsClient>(client =>
        {
            var url = configuration["Services:Tickets:Url"];
            client.BaseAddress = new Uri(url!);
        });

        services.AddHttpClient<ITranslationsClient, TranslationsClient>(client =>
        {
            var url = configuration["Services:Translations:Url"];
            client.BaseAddress = new Uri(url!);
        });

        services.AddHttpClient<IPersonalInfoVaultClient, PersonalInfoVaultClient>(client =>
        {
            var url = configuration["Services:PersonalInfoVault:Url"];
            client.BaseAddress = new Uri(url!);
        });

        return services;
    }
}