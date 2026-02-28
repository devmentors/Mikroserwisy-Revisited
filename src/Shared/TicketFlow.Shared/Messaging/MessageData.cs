namespace TicketFlow.Shared.Messaging;

public record MessageData(Guid MessageId, byte[] Payload, string Type) : IMessage;