using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Communication.Alerting;

public static class AlertMessageConventions
{
    public const string AlertingSuffix = ".alert";
    
    public static string WithAlertingApplied<TMessage>(this string routingKey) where TMessage: class, IMessage
    {
        if (typeof(IAlertMessage).IsAssignableFrom(typeof(TMessage)))
        {
            return routingKey + AlertingSuffix;
        }

        return routingKey;
    }
}