using TicketFlow.Shared.Messaging.Resiliency;

namespace TicketFlow.Shared.Messaging;

public static class MessageTypeName
{
    public static string CreateFor<TMessage>() where TMessage : IMessage
    {
        var messageType = typeof(TMessage);
        
        if (messageType.IsGenericType && messageType.GetGenericTypeDefinition() == typeof(Fault<>))
        {
            var wrappedMessageType = messageType.GetGenericArguments()[0];
            return "Fault." + wrappedMessageType.Name;
        }
        return messageType.Name;
    }
    
}