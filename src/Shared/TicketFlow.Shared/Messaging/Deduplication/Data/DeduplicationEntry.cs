namespace TicketFlow.Shared.Messaging.Deduplication.Data;

public class DeduplicationEntry
{
    public string MessageId { get; init; }
    public DateTimeOffset ProcessedAt { get; init; }
}