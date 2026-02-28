using TicketFlow.Shared.Commands;

namespace TicketFlow.Shared.Messaging.Executor;

internal sealed class CommandHandlerExecutorDecorator<TCommand>(ICommandHandler<TCommand> handler, IMessageExecutor executor) 
    : ICommandHandler<TCommand> where TCommand : class, ICommand
{
    public async Task HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        await executor.ExecuteAsync(async () => await handler.HandleAsync(command, cancellationToken), cancellationToken);
    }
}