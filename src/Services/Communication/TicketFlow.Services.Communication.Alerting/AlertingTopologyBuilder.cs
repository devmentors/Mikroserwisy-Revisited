using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Services.Communication.Alerting;

public class AlertingTopologyBuilder(ITopologyBuilder topologyBuilder, IMessagePublisherConventionProvider publisherConventionProvider)
{
    public const string AlertsExchange = "alerting";
    
    public async Task CreateTopologyAsync(CancellationToken cancellationToken)
    {
        var alertMessageTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x is { IsClass: true, IsAbstract: false } && typeof(IAlertMessage).IsAssignableFrom(x));

        foreach (var messageType in alertMessageTypes)
        {
            var (publisherDestination, _) = ((string, string)) typeof(IMessagePublisherConventionProvider)
                .GetMethod(nameof(IMessagePublisherConventionProvider.Get))
                .MakeGenericMethod(messageType)
                .Invoke(publisherConventionProvider, null);

            var routingKey = "#." + AlertMessageConventions.AlertingSuffix + ".#";
            routingKey = routingKey.Replace("..", ".");
            
            await topologyBuilder.CreateTopologyAsync(
                publisherDestination,
                AlertsExchange,
                TopologyType.PublisherToPublisher,
                filter: routingKey,
                cancellationToken: cancellationToken);
        }
    }
}