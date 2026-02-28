namespace TicketFlow.Shared.AnomalyGeneration.CodeApi;

public interface IConsumerAnomalyGenerator
{
    Task GenerateBeforeHandlerAsync<TMessage>(TMessage message);
    Task GenerateAfterHandlerAsync<TMessage>(TMessage message);
}