using Microsoft.EntityFrameworkCore;

namespace TicketFlow.Shared.Messaging.Deduplication.Data;

internal sealed class PostgresDeduplicationStore(DeduplicationDbContext deduplicationDbContext) : IDeduplicationStore
{
    public Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default)
        => deduplicationDbContext.DeduplicationEntries.AnyAsync(x => x.MessageId == messageId, cancellationToken);

    public async Task AddEntryAsync(string messageId, CancellationToken cancellationToken = default)
    {
        var deduplicationEntry = new DeduplicationEntry { MessageId = messageId, ProcessedAt = DateTimeOffset.UtcNow };
        await deduplicationDbContext.DeduplicationEntries.AddAsync(deduplicationEntry, cancellationToken);
        await deduplicationDbContext.SaveChangesAsync(cancellationToken);
    }
}