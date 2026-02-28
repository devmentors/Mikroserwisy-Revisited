using TicketFlow.Shared.Messaging;

namespace TicketFlow.Shared.AnomalyGeneration.CodeApi;

internal sealed class MessageHandlerAnomalyDecorator<TMessage>(
    IMessageHandler<TMessage> messageHandler,
    IConsumerAnomalyGenerator anomalies) 
    : IMessageHandler<TMessage> where TMessage : class, IMessage
{
    public async Task HandleAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        await anomalies.GenerateBeforeHandlerAsync(message);
        await messageHandler.HandleAsync(message, cancellationToken);
        await anomalies.GenerateAfterHandlerAsync(message);
    }
}