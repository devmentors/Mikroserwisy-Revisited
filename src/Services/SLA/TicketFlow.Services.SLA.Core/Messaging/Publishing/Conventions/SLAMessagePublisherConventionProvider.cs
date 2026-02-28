using TicketFlow.Services.Communication.Alerting;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.SLA.Core.Messaging.Publishing.Conventions;

internal sealed class SLAMessagePublisherConventionProvider : IMessagePublisherConventionProvider
{
    public const string ExchangeName = "sla-exchange";
    
    public (string destination, string routingKey) Get<TMessage>() where TMessage : class, IMessage 
        => (ExchangeName, PascalToKebabCase(typeof(TMessage).Name).WithAlertingApplied<TMessage>());

    private static string PascalToKebabCase(string str)
    {
        return string.Concat(str.SelectMany(ConvertChar));

        IEnumerable<char> ConvertChar(char c, int index)
        {
            if (char.IsUpper(c) && index != 0) yield return '-';
            yield return char.ToLower(c);
        }
    }
}