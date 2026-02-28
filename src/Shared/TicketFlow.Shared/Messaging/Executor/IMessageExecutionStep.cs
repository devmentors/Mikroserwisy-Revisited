namespace TicketFlow.Shared.Messaging.Executor;

internal interface IMessageExecutionStep
{
    ExecutionType Type { get; }
    Task ExecuteAsync(MessageProperties messageProperties, Func<Task> next, CancellationToken cancellationToken = default);
}

public enum ExecutionType
{
    BeforeTransaction,
    WithinTransaction,
    AfterTransaction,
}