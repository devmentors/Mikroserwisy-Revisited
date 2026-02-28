using TicketFlow.Shared.Messaging.Executor;
using TicketFlow.Shared.Messaging.Outbox.Data;

namespace TicketFlow.Shared.Messaging.Outbox;

internal sealed class OutboxBeforeExecutionStep(OutboxLocalCache cache) : IMessageExecutionStep
{
    public ExecutionType Type => ExecutionType.BeforeTransaction;
    
    public async Task ExecuteAsync(MessageProperties messageProperties, Func<Task> next, CancellationToken cancellationToken = default)
    {
        cache.Initialize();
        await next();
    }
}