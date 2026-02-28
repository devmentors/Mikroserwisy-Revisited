using Microsoft.Extensions.Logging;
using TicketFlow.Shared.Messaging.Executor;

namespace TicketFlow.Shared.Messaging.Deduplication;

internal sealed class DeduplicationBeforeExecutionStep(
    IDeduplicationStore deduplicationStore, 
    ILogger<DeduplicationBeforeExecutionStep> logger) 
    : IMessageExecutionStep
{
    public ExecutionType Type => ExecutionType.BeforeTransaction;

    public async Task ExecuteAsync(MessageProperties messageProperties, Func<Task> next, CancellationToken cancellationToken)
    {
        var messageId = messageProperties.MessageId;

        if (await deduplicationStore.ExistsAsync(messageId, cancellationToken))
        {
            throw new MessageExecutionAbortedException($"Message with id: {messageId} is already processed.");
        }

        await next();
    }
}