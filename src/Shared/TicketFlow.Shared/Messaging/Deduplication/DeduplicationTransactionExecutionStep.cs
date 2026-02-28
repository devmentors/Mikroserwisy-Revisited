using TicketFlow.Shared.Messaging.Executor;

namespace TicketFlow.Shared.Messaging.Deduplication;

internal sealed class DeduplicationTransactionExecutionStep(IDeduplicationStore deduplicationStore) : IMessageExecutionStep
{
    public ExecutionType Type => ExecutionType.WithinTransaction;

    public async Task ExecuteAsync(MessageProperties messageProperties, Func<Task> next, CancellationToken cancellationToken)
    {
        var messageId = messageProperties.MessageId;
        await deduplicationStore.AddEntryAsync(messageId, cancellationToken);
        await next();
    }
}