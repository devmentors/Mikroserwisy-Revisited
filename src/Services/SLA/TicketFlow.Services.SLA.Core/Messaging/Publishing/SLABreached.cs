using TicketFlow.Services.Communication.Alerting;
using TicketFlow.Services.SLA.Core.Data.Models;

namespace TicketFlow.Services.SLA.Core.Messaging.Publishing;

public record SLABreached(ServiceType ServiceType, string ServiceSourceId) : IAlertMessage
{
    public string AlertMessageContent =>
        $"SLA niespełnione dla [{ServiceType.ToHumanReadableString()}]: {ServiceSourceId})";

    public string AlertType => "SLA";
}