using RabbitMQ.Client;

namespace TicketFlow.Shared.Messaging.RabbitMQ;

internal sealed class ConnectionProvider(IConnection consumerConnection, IConnection producerConnection)
{
    public IConnection ConsumerConnection { get; } = consumerConnection;
    public IConnection ProducerConnection { get; } = producerConnection;
}