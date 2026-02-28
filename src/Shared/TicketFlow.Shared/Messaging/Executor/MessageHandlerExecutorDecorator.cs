namespace TicketFlow.Shared.Messaging.Executor;

internal sealed class MessageHandlerExecutorDecorator<TMessage>(IMessageHandler<TMessage> handler, IMessageExecutor executor) 
    : IMessageHandler<TMessage> where TMessage : class, IMessage
{
    public async Task HandleAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        await executor.ExecuteAsync(async () => await handler.HandleAsync(message, cancellationToken), cancellationToken);
    }
}