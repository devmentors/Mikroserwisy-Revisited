using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TicketFlow.Shared.Messaging.Outbox.Data;

internal sealed class OutboxLocalCache(MessagePropertiesAccessor messagePropertiesAccessor, ILogger<OutboxLocalCache> logger)
{
    private readonly IDictionary<string,OutboxPendingMessages> _cache = new ConcurrentDictionary<string, OutboxPendingMessages>();

    public IReadOnlyList<OutboxMessage> GetForPublish()
    {
        var messageId = messagePropertiesAccessor.Get()?.MessageId;

        if (messageId is null)
        {
            return Array.Empty<OutboxMessage>();
        }
        
        var hasPendingMessages = _cache.TryGetValue(messageId, out var result);
        _cache.Remove(messageId);
        
        logger.LogInformation($"Outbox local cache for messageId: {messageId} returns {result!.Messages.Count} messages");
        return hasPendingMessages ? result!.Messages : Array.Empty<OutboxMessage>();
    }

    public void Initialize()
    {
        var messageId = messagePropertiesAccessor.Get()?.MessageId;

        if (messageId is null || _cache.ContainsKey(messageId))
        {
            return;
        }
        
        _cache.Add(messageId, new OutboxPendingMessages());
        logger.LogInformation($"Outbox local cache initialized for messageId: {messageId}");
    }

    public void Add(OutboxMessage message)
    {
        var messageId = messagePropertiesAccessor.Get()?.MessageId;
        
        if (messageId is null)
        {
            return;
        }
        
        _cache.TryGetValue(messageId, out var result);
        result!.Messages.Add(message);
    }
    
    private class OutboxPendingMessages
    {
        public List<OutboxMessage> Messages { get; } = new();
    }
}