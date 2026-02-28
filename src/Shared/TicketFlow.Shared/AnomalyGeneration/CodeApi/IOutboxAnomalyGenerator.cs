using TicketFlow.Shared.Messaging.Outbox;

namespace TicketFlow.Shared.AnomalyGeneration.CodeApi;

public interface IOutboxAnomalyGenerator
{
    Task GenerateOnSaveAsync(OutboxMessage message);
    Task GenerateOnPublishAsync(OutboxMessage message);
}