namespace TicketFlow.Shared.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<TMessage>(TMessage message, string? destination = default, string? routingKey = default, string? messageId = default,
        IDictionary<string, object>? headers = default, CancellationToken cancellationToken = default) where TMessage : class, IMessage;
}