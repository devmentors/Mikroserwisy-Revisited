using Microsoft.Extensions.Logging;

namespace TicketFlow.Shared.Messaging.Outbox;

internal sealed class OutboxMessagePublisher(IMessageOutbox messageOutbox, ILogger<OutboxMessagePublisher> logger, MessagePropertiesAccessor propertiesAccessor) : IMessagePublisher
{
    public async Task PublishAsync<TMessage>(TMessage message, string? destination = default,
        string? routingKey = default, string? messageId = default,
        IDictionary<string, object>? headers = default, CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
    {
        var messageProperties = propertiesAccessor.Get();
        var messageIdSafe = messageProperties?.MessageId ?? messageId ?? Guid.NewGuid().ToString();
        
        await messageOutbox.AddAsync(message, messageIdSafe, destination, routingKey, messageProperties?.Headers ?? headers, cancellationToken);
        logger.LogInformation($"Save message to outbox: {typeof(TMessage).Name}");
    }
}