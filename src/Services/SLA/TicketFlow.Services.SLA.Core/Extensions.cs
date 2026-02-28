using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.CourseUtils;
using TicketFlow.Services.SLA.Core.Data;
using TicketFlow.Services.SLA.Core.Data.Repositories;
using TicketFlow.Services.SLA.Core.Http;
using TicketFlow.Services.SLA.Core.Http.Billing;
using TicketFlow.Services.SLA.Core.Http.Communication;
using TicketFlow.Services.SLA.Core.Http.Tickets;
using TicketFlow.Services.SLA.Core.Initializers;
using TicketFlow.Services.SLA.Core.Messaging;
using TicketFlow.Services.SLA.Core.Messaging.Consuming;
using TicketFlow.Services.SLA.Core.Messaging.Consuming.ApplicationServices;
using TicketFlow.Services.SLA.Core.Messaging.Consuming.Demultiplexing;
using TicketFlow.Services.SLA.Core.Messaging.Consuming.Partitioning;
using TicketFlow.Services.SLA.Core.Messaging.Publishing.Conventions;
using TicketFlow.Services.SLA.Core.Schedulers;
using TicketFlow.Services.SystemMetrics.Generator;
using TicketFlow.Shared.AnomalyGeneration;
using TicketFlow.Shared.App;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Data;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Deduplication;
using TicketFlow.Shared.Messaging.Ordering.OutOfOrderDetection;
using TicketFlow.Shared.Messaging.Outbox;
using TicketFlow.Shared.Messaging.Partitioning;
using TicketFlow.Shared.Messaging.RabbitMQ;
using TicketFlow.Shared.Messaging.Resiliency;
using TicketFlow.Shared.Messaging.Topology;
using TicketFlow.Shared.Metrics;
using TicketFlow.Shared.Observability;
using TicketFlow.Shared.Queries;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.Services.SLA.Core;

public static class Extensions
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddExceptions()
            .AddApp(configuration)
            .AddSerialization(configuration)
            .AddAppInitializers()
            .AddCommands()
            .AddQueries()
            .AddLogging()
            .AddMessaging(configuration, x => x
                .UseRabbitMq()
                .UseMessagePublisherConvention<SLAMessagePublisherConventionProvider>()
                .UseMessageConsumerConvention<DontUseConventionalTopology>()
                .UseDeduplication()
                .UseOutbox()
                .UseResiliency()
                .UseOutOfOrderDetection()
                .UseAnomalies())
            .AddPostgres<SLADbContext>(configuration)
            .AddSystemMetrics(configuration)
            .AddMetrics(configuration)
            .AddObservability(configuration);

        services.AddHttpClient<ITicketsClient, TicketsClient>(builder =>
        {
            builder.BaseAddress = new Uri(configuration.GetValue<string>("Services:Tickets"));
        });
        
        services.AddHttpClient<ICommunicationClient, CommunicationsClient>(builder =>
        {
            builder.BaseAddress = new Uri(configuration.GetValue<string>("Services:Communication"));
        });

        services.AddHttpClient<IBillingClient, BillingClient>(builder =>
        {
            builder.BaseAddress = new Uri(
                configuration.GetValue<string>("Services:BillingIntegration")
                ?? "http://localhost:6000");
            builder.Timeout = TimeSpan.FromSeconds(5);
        });

        services.AddHostedService<SLAConsumerService>();
        services.AddHostedService<SLATopologyInitializer>();
        services.AddTransient<ISLARepository, SLARepository>();
        services.AddHostedService<RemindersWatcher>();
        services.AddSingleton(new DeadlineBreachOptions
        {
            WatcherIntervalInSeconds = 5,
            SecondsBetweenBreachAlerts = 120
        });
        services.AddHostedService<DeadlineBreachWatcher>();
        services.AddScoped<TicketService>();
        services.AddScoped<TicketChangesHandler>();

        if (FeatureFlags.UsePartitioningExample)
        {
            // Taken from environment variables or from execution params from CLI
            // EXAMPLE: dotnet run --PartitionNums:0 1 --PartitionNums:1 3
            var partitionNumbers = configuration.GetValue<string>("PartitionNums");
            if (string.IsNullOrEmpty(partitionNumbers))
            {
                partitionNumbers = "1";
            }
            var partitionNumbersParsed = partitionNumbers.Split(',').Select(int.Parse).ToArray();
            
            PartitioningOptions partitioningOpts = PartitioningOptions.Default;

            var section = configuration.GetSection("Partitioning:TicketChanges");
            if (section.Exists())
            {
                section.Bind(partitioningOpts, opts => { opts.BindNonPublicProperties = true; });
            }

            if (FeatureFlags.ExtendPartitioningExample)
            {
                partitionNumbersParsed = Enumerable.Range(1, partitioningOpts.NumberOfPartitions).ToArray();
                partitioningOpts = partitioningOpts with { OnlyOneActiveConsumerPerPartition = false };
            }

            if (partitionNumbers is null || partitionNumbers.Length == 0)
            {
                Console.Error.WriteLine("No partition numbers specified - will choose partition '1' as fallback.");
                partitionNumbersParsed = [1];
            }

            Console.WriteLine(
                $"Partitions handled for {nameof(TicketChangesPartitioningSetup)} are: {string.Join(",", partitionNumbersParsed)}");
            
            services.AddSingleton(new TicketChangesPartitioningSetup(partitionNumbersParsed.ToList(), partitioningOpts));
        }
        else
        {
            services.AddSingleton(new TicketChangesPartitioningSetup(default, default));
        }

        return services;
    }
}