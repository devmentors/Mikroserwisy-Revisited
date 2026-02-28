using Microsoft.Extensions.Options;
using TicketFlow.Shared.App;

namespace TicketFlow.Shared.Messaging.RabbitMQ;

internal sealed class RabbitMqDefaultMessageConventionProvider(IOptions<AppOptions> options) 
    : IMessageConsumerConventionProvider, IMessagePublisherConventionProvider
{
    (string destination, string routingKey) IMessagePublisherConventionProvider.Get<TMessage>()
    {
        var transformedMessageName = PascalToKebabCase(typeof(TMessage).Name);
        var destination = $"{transformedMessageName}-exchange";
        return (destination, "");
    }

    (string destination, string routingKey) IMessageConsumerConventionProvider.Get<TMessage>()
    {
        var transformedMessageName = PascalToKebabCase(typeof(TMessage).Name);
        var destination = $"{transformedMessageName}-{options.Value.AppName.ToLowerInvariant()}-queue";
        return (destination, "");
    }
    
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