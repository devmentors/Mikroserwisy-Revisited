using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TicketFlow.Shared.Messaging.Resiliency;

public static class Extensions
{
    private static ResiliencyOptions DefaultOpts = new ResiliencyOptions(
        new ConsumerResiliencyOptions(
            BrokerRetriesEnabled: false,
            BrokerRetriesLimit: 3,
            ConsumerRetriesEnabled: false,
            ConsumerRetriesLimit: 3,
            UseDeadletter: false,
            PublishFaultOnFailure: false,
            MaxMessagesFetchedPerConsumer: 5),
        new ProducerResiliencyOptions(PublisherConfirmsEnabled: false, PublishMandatoryEnabled: false));

    public static IMessagingRegisterer UseResiliency(this IMessagingRegisterer registerer)
    {
        var services = registerer.Services;
        var configuration = registerer.Configuration;

        ResiliencyOptions options = DefaultOpts;

        var section = configuration.GetSection("Resiliency");
        if (section.Exists())
        {
            section.Bind(options, opts => { opts.BindNonPublicProperties = true; });
        }
        
        services.AddSingleton(options);
        services.AddSingleton<ReliablePublishing>();
        services.AddSingleton<ReliableConsuming>();

        if (options.Consumer.ConsumerRetriesEnabled)
        {
            registerer.Services.TryDecorate(typeof(IMessageHandler<>), typeof(MessageHandlerRetryDecorator<>));
        }

        return registerer;
    }
}