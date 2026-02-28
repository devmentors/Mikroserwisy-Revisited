using TicketFlow.Services.Communication.Alerting;
using TicketFlow.Shared.Messaging.Partitioning;

namespace TicketFlow.Services.Tickets.Core.Messaging.Publishing;

public record IncidentCreated(Guid TicketId, int Version) : IAlertMessage, IMessageWithPartitionKey
{
    public string AlertMessageContent => $"Utworzono incydent - identyfikator ticketa: {TicketId}";
    public string AlertType => "Incydent";
    public string PartitionKey => TicketId.ToString();
}