using TicketFlow.Shared.Exceptions;

namespace TicketFlow.Shared.Messaging.Outbox;

public sealed class OutboxMessage
{
    public required Guid Id { get; init; }
    public required string MessageId { get; init; }
    public required string SerializedMessage { get; init; }
    public required object Message { get; init; } 
    public required string MessageType { get; init; } 
    public required DateTimeOffset StoredAt { get; init; }

    public string? Destination { get; init; } 
    public string? RoutingKey { get; init; } 
    public IDictionary<string, object> Headers { get; init; } = new Dictionary<string, object>();
    public DateTimeOffset? ProcessedAt { get; private set; }
    
    public void MarkAsProcessed()
    {
        if (ProcessedAt is not null)
        {
            throw new TicketFlowException($"Cannot change processed datetime for outbox message.");
        }
        
        ProcessedAt = DateTimeOffset.UtcNow;
    }
}