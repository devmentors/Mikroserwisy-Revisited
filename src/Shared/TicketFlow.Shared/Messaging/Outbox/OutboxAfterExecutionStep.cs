using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketFlow.Shared.Messaging.Executor;
using TicketFlow.Shared.Messaging.Outbox.Data;

namespace TicketFlow.Shared.Messaging.Outbox;

internal sealed class OutboxAfterExecutionStep(OutboxLocalCache cache, OutboxPublishChannel outboxPublishChannel, IOptions<OutboxOptions> outboxOptions, 
    ILogger<OutboxAfterExecutionStep> logger) : IMessageExecutionStep
{
    public ExecutionType Type => ExecutionType.AfterTransaction;
    
    public async Task ExecuteAsync(MessageProperties messageProperties, Func<Task> next, CancellationToken cancellationToken = default)
    {
        if (outboxOptions.Value.PublishOnCommit)
        {
            var outboxMessages = cache.GetForPublish();
            logger.LogInformation("Publish on commit enabled. Publishing {MessagesNumber} outbox messages...", outboxMessages.Count);

            foreach (var outboxMessage in outboxMessages)
            {
                await outboxPublishChannel.PublishAsync(outboxMessage, cancellationToken);
            }
        }
    }
}