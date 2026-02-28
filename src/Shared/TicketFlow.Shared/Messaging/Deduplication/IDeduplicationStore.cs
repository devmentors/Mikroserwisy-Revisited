using TicketFlow.Shared.Messaging.Deduplication.Data;

namespace TicketFlow.Shared.Messaging.Deduplication;

public interface IDeduplicationStore
{
    Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default);
    Task AddEntryAsync(string messageId, CancellationToken cancellationToken = default);
}