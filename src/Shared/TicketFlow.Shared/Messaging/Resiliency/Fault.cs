namespace TicketFlow.Shared.Messaging.Resiliency;

public record Fault<TMessage>(
    TMessage FaultedMessage,
    string ExceptionType,
    string ExceptionMessage,
    string FailedOnQueue) : IMessage;
    
public record Fault(
    string FaultedMessage,
    string ExceptionType,
    string ExceptionMessage,
    string FailedOnQueue) : IMessage;