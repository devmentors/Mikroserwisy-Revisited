using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Aggregation.Core.Messaging.Consuming;

public record DeadlinesCalculated(string ServiceType, string ServiceSourceId, DateTimeOffset DeadlineUtc) : IMessage;
