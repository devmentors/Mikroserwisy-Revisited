namespace TicketFlow.Shared.Messaging;

public interface IMessagePublisherConventionProvider
{
    (string destination, string routingKey) Get<TMessage>() where TMessage : class, IMessage;
}

public interface IMessageConsumerConventionProvider
{
    (string destination, string routingKey) Get<TMessage>() where TMessage : class, IMessage;
}