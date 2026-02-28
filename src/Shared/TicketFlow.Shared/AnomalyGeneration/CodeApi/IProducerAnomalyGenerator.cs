using TicketFlow.Shared.Messaging.Executor;

namespace TicketFlow.Shared.AnomalyGeneration.CodeApi;

public interface IProducerAnomalyGenerator
{
    Task GenerateOnProduceAsync(string messageType, ExecutionType when);
}