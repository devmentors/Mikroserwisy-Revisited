using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.Messaging.Consuming.DeadlinesCalculated;

public record DeadlinesCalculated(string ServiceType, string ServiceSourceId, DateTimeOffset DeadlineUtc) : IMessage;