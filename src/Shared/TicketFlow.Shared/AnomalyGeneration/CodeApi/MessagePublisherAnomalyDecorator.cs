using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Outbox;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.Shared.AnomalyGeneration.CodeApi;

internal sealed class MessagePublisherAnomalyDecorator(
    IMessagePublisher messagePublisher, 
    IOutboxAnomalyGenerator anomalies, 
    ISerializer serializer,
    AnomalyContextAccessor contextAccessor) : IMessagePublisher
{
    public async Task PublishAsync<TMessage>(TMessage message, string? destination = default, string? routingKey = default,
        string? messageId = default, IDictionary<string, object>? headers = default, CancellationToken cancellationToken = default) where TMessage : class, IMessage
    {
        contextAccessor.InitializeIfEmpty();
        contextAccessor.Get().DetectedMessageTypes.Add(MessageTypeName.CreateFor<TMessage>());
        
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            Destination = destination,
            RoutingKey = routingKey,
            Headers = headers ?? new Dictionary<string, object>(),
            MessageType = message.GetType().AssemblyQualifiedName,
            Message = message,
            SerializedMessage = serializer.Serialize(message),
            StoredAt = DateTimeOffset.UtcNow
        };
        
        await messagePublisher.PublishAsync(message, destination, routingKey, messageId, headers, cancellationToken);
        await anomalies.GenerateOnPublishAsync(outboxMessage);
    }
}