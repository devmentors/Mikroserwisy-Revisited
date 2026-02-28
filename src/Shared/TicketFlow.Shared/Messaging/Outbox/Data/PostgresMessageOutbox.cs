using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketFlow.Shared.AnomalyGeneration.CodeApi;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.Shared.Messaging.Outbox.Data;

internal sealed class PostgresMessageOutbox(OutboxDbContext dbContext, OutboxLocalCache cache, ISerializer serializer, ILogger<PostgresMessageOutbox> logger) : IMessageOutbox
{
    public async Task AddAsync<TMessage>(TMessage message, string messageId, string? destination = default, string? routingKey = default,
        IDictionary<string, object>? headers = default, CancellationToken cancellationToken = default) where TMessage : IMessage
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            Destination = destination,
            RoutingKey = routingKey,
            Headers = headers ?? new Dictionary<string, object>(),
            MessageType = message.GetType().AssemblyQualifiedName,
            Message = message,
            SerializedMessage = serializer.Serialize(message),
            StoredAt = DateTimeOffset.UtcNow
        };
        
        await dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        cache.Add(outboxMessage);
        logger.LogInformation("Message with id: {MessageId} added to outbox", outboxMessage.Id);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetUnsentAsync(int batchSize = default, CancellationToken cancellationToken = default)
    {
        var sqlQuery = $"SELECT * FROM outbox.\"OutboxMessages\" WHERE \"ProcessedAt\" IS NULL ORDER BY \"StoredAt\" DESC {(batchSize > 0? $"LIMIT {batchSize}" : "")} FOR UPDATE SKIP LOCKED";
        
        return await dbContext.OutboxMessages
            .FromSqlRaw(sqlQuery)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken = default)
    {
        outboxMessage.MarkAsProcessed();
        dbContext.OutboxMessages.Update(outboxMessage);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}