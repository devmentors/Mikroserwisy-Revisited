using TicketFlow.Services.SLA.Core.Data.Models;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.SLA.Core.Messaging.Publishing;

public record DeadlinesCalculated(ServiceType ServiceType, string ServiceSourceId, DateTimeOffset DeadlineUtc) : IMessage;