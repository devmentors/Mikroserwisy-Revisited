using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Communication.Alerting;

public interface IAlertMessage : IMessage
{
    string AlertMessageContent { get; }
    string AlertType { get; }
}