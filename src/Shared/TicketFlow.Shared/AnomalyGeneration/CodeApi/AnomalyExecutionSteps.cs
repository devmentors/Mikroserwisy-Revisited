using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Messaging.Executor;

namespace TicketFlow.Shared.AnomalyGeneration.CodeApi;

internal sealed class AnomalyBeforeExecutionSteps(
    IProducerAnomalyGenerator anomalies,
    AnomalyContextAccessor contextAccessor) : IMessageExecutionStep
{
    public ExecutionType Type => ExecutionType.BeforeTransaction;

    public async Task ExecuteAsync(MessageProperties messageProperties, Func<Task> next, CancellationToken cancellationToken = default)
    {
        foreach (var messageType in contextAccessor.Get()?.DetectedMessageTypes ?? [])
        {
            await anomalies.GenerateOnProduceAsync(messageType, ExecutionType.BeforeTransaction);   
        }
        await next();
    }
}

internal sealed class AnomalyWithinTransactionExecutionStep(
    IProducerAnomalyGenerator anomalies,
    AnomalyContextAccessor contextAccessor) : IMessageExecutionStep
{
    public ExecutionType Type => ExecutionType.WithinTransaction;

    public async Task ExecuteAsync(MessageProperties messageProperties, Func<Task> next, CancellationToken cancellationToken = default)
    {
        foreach (var messageType in contextAccessor.Get()?.DetectedMessageTypes ?? [])
        {
            await anomalies.GenerateOnProduceAsync(messageType, ExecutionType.WithinTransaction);   
        }
        await next();
    }
}

internal sealed class AnomalyAfterExecutionStep(
    IProducerAnomalyGenerator anomalies,
    AnomalyContextAccessor contextAccessor) : IMessageExecutionStep
{
    public ExecutionType Type => ExecutionType.AfterTransaction;

    public async Task ExecuteAsync(MessageProperties messageProperties, Func<Task> next, CancellationToken cancellationToken = default)
    {
        foreach (var messageType in contextAccessor.Get()?.DetectedMessageTypes ?? [])
        {
            await anomalies.GenerateOnProduceAsync(messageType, ExecutionType.AfterTransaction);   
        }
        await next();
    }
}