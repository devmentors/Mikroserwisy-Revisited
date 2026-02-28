using TicketFlow.Shared.Messaging.Partitioning;

namespace TicketFlow.Shared.Messaging;

public interface IMessageConsumer
{
    Task<IMessageConsumer> ConsumeMessage<TMessage>(
        Func<TMessage, Task>? handle = default,
        string? queue = default,
        string[]? acceptedMessageTypes = default,
        CancellationToken cancellationToken = default) where TMessage : class, IMessage;
    
    Task<IMessageConsumer> ConsumeNonGeneric(
        Func<MessageData, Task> handleRawPayload,
        string queue,
        string[]? acceptedMessageTypes = default,
        CancellationToken cancellationToken = default);

    
    Task GetMessage<TMessage>(
        Func<TMessage, Task> handle,
        string? queue = default,
        CancellationToken cancellationToken = default) where TMessage : class, IMessage;

    Task<IMessageConsumer> ConsumeMessageFromPartitions<TMessage>(
        IConsumerSpecificPartitioningSetup consumerPartitioningSetup,
        Func<TMessage, Task>? handle = default,
        string? queue = default,
        string[]? acceptedMessageTypes = default,
        CancellationToken cancellationToken = default) where TMessage : class, IMessage;

    Task<IMessageConsumer> ConsumeNonGenericFromPartitions(
        IConsumerSpecificPartitioningSetup consumerPartitioningSetup,
        Func<MessageData, Task> handleRawPayload,
        string? queue,
        string[]? acceptedMessageTypes = default,
        CancellationToken cancellationToken = default);
}