using TicketFlow.Services.Communication.Alerting;

namespace TicketFlow.Services.Communication.Core.Messaging.Consuming;

public class ProducerAgnosticAlertMessage : IAlertMessage
{
    public string AlertMessageContent { get; set; }
    public string AlertType { get; set; }
}