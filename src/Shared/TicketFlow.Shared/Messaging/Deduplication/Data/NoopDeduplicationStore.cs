namespace TicketFlow.Shared.Messaging.Deduplication.Data;

internal sealed class NoopDeduplicationStore : IDeduplicationStore
{
    public Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task AddEntryAsync(string messageId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}