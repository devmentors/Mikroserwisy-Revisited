namespace TicketFlow.Shared.Messaging.Topology;

public class DontUseConventionalTopology : IMessageConsumerConventionProvider
{
    public (string destination, string routingKey) Get<TMessage>() where TMessage : class, IMessage
    {
        return (default, default);
    }
}