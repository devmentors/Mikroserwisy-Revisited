using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TicketFlow.Shared.DependencyInjection;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Shared.Messaging.RabbitMQ;

public static class Extensions
{
    private const string SectionName = "rabbitMq";
    
    public static IMessagingRegisterer UseRabbitMq(this IMessagingRegisterer registerer, string sectionName = SectionName)
    {
        var services = registerer.Services;
        var section = registerer.Configuration.GetSection(sectionName);
        var appName = registerer.Configuration.GetValue<string>("App:AppName");
        services.Configure<RabbitMqOptions>(section);
        
        services.AddSingleton(svc =>
        {
            var options = new RabbitMqOptions();
            section.Bind(options);
            
            var factory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.Username,
                Password = options.Password,
                VirtualHost = options.VirtualHost
            };
        
            var consumerConnection = factory.CreateConnection($"{appName}-consumer");
            var producerConnection = factory.CreateConnection($"{appName}-producer");
            var connectionProvider = new ConnectionProvider(consumerConnection, producerConnection);
            return connectionProvider;
        });
        services.AddTransient<ChannelFactory>();

        services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
        services.AddSingleton<IMessageConsumer, RabbitMqMessageConsumer>();
        services.AddSingleton<IMessageConsumerConventionProvider, RabbitMqDefaultMessageConventionProvider>();
        services.AddSingleton<IMessagePublisherConventionProvider, RabbitMqDefaultMessageConventionProvider>();
        services.AddTransient<ITopologyBuilder, RabbitMqTopologyBuilder>();
        services.AddSingleton(provider => new TopologyOptions(
            CreateTopology: provider.GetRequiredService<IOptions<RabbitMqOptions>>().Value.CreateTopology));
        services.AddHostedService<RabbitMqTopologyInitializer>();
        return registerer;
    }

    public static IMessagingRegisterer UseMessageConsumerConvention<TProvider>(this IMessagingRegisterer registerer)
        where TProvider : class, IMessageConsumerConventionProvider
        => UseConvention<IMessageConsumerConventionProvider, TProvider>(registerer);
    
    public static IMessagingRegisterer UseMessagePublisherConvention<TProvider>(this IMessagingRegisterer registerer) 
        where TProvider : class, IMessagePublisherConventionProvider
        => UseConvention<IMessagePublisherConventionProvider, TProvider>(registerer);
    
    private static IMessagingRegisterer UseConvention<TInterface, TProvider>(this IMessagingRegisterer registerer) where TProvider : class, TInterface where TInterface : class
    {
        registerer.Services.ReplaceWithSingletonService<TInterface, TProvider>();
        return registerer;
    }
}