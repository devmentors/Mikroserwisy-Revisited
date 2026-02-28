namespace TicketFlow.Shared.Messaging.Executor;

internal sealed class MessageExecutionAbortedException(string message) : Exception(message);