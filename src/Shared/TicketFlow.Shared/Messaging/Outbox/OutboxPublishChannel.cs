using System.Threading.Channels;

namespace TicketFlow.Shared.Messaging.Outbox;

internal sealed class OutboxPublishChannel
{
    private readonly Channel<OutboxMessage> _channel = Channel.CreateUnbounded<OutboxMessage>();
    
    public ValueTask PublishAsync(OutboxMessage message, CancellationToken cancellationToken) 
        => _channel.Writer.WriteAsync(message, cancellationToken);
    
    public IAsyncEnumerable<OutboxMessage> GetAsync(CancellationToken cancellationToken) 
        => _channel.Reader.ReadAllAsync(cancellationToken);
}