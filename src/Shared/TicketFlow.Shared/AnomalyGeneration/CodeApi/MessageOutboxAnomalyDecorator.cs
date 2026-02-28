using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Outbox;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.Shared.AnomalyGeneration.CodeApi;

internal sealed class MessageOutboxAnomalyDecorator(
    IMessageOutbox messageOutbox,
    IOutboxAnomalyGenerator anomalies,
    ISerializer serializer,
    AnomalyContextAccessor contextAccessor) : IMessageOutbox
{
    public Task<IReadOnlyList<OutboxMessage>> GetUnsentAsync(int batchSize = default, CancellationToken cancellationToken = default)
        => messageOutbox.GetUnsentAsync(batchSize, cancellationToken);

    public async Task AddAsync<TMessage>(TMessage message, string messageId, string? destination = default, string? routingKey = default,
        IDictionary<string, object>? headers = default, CancellationToken cancellationToken = default) where TMessage : IMessage
    {
        await messageOutbox.AddAsync(message, messageId, destination, routingKey, headers, cancellationToken);

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
        
        await anomalies.GenerateOnSaveAsync(outboxMessage);
    }

    public Task MarkAsProcessedAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken = default)
        => messageOutbox.MarkAsProcessedAsync(outboxMessage, cancellationToken);
}