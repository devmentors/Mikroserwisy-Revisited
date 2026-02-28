using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Aggregation.Core.Messaging.Consuming;

public record SLABreached(string ServiceType, string ServiceSourceId) : IMessage;
