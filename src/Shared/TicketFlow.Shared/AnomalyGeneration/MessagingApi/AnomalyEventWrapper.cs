using TicketFlow.Shared.Messaging;

namespace TicketFlow.Shared.AnomalyGeneration.MessagingApi;

// TODO:
// This is needed due to the fact, that there only exists IMessageConsumer.ConsumeMessage<TMessage>
// In theory one could instead implement non-generic ConsumeMessage that accept mandatory delegate with (JSON + messageType) input,
// so that you can demultiplex messages on your own when consuming and route them to specific handler
public record AnomalyEventWrapper(AnomalyEnabled? Enabled, AnomalyDisabled? Disabled) : IMessage;