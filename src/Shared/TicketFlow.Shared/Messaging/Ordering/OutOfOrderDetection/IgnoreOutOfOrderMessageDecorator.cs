using Microsoft.Extensions.Logging;

namespace TicketFlow.Shared.Messaging.Ordering.OutOfOrderDetection;

public class IgnoreOutOfOrderMessageDecorator<TMessage> : IMessageHandler<TMessage> where TMessage : class, IMessage
{
    private readonly IMessageHandler<TMessage> _decorated;
    private readonly OutOfOrderDetector _outOfOrderDetector;
    private readonly ILogger<IgnoreOutOfOrderMessageDecorator<TMessage>> _logger;

    public IgnoreOutOfOrderMessageDecorator(
        IMessageHandler<TMessage> decorated,
        OutOfOrderDetector outOfOrderDetector,
        ILogger<IgnoreOutOfOrderMessageDecorator<TMessage>> logger)
    {
        _decorated = decorated ?? throw new ArgumentNullException(nameof(decorated));
        _outOfOrderDetector = outOfOrderDetector;
        _logger = logger;
    }
    
    public async Task HandleAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        if (typeof(IVersionedMessage).IsAssignableFrom(typeof(TMessage)) is false)
        {
            await _decorated.HandleAsync(message, cancellationToken);
            return;
        }
        
        var isOutOfOrder = await _outOfOrderDetector.Check(message);
        if (isOutOfOrder)
        {
            _logger.LogWarning($"[{DateTime.UtcNow:O}] Detected ouf of order message - skipping! Message:{Environment.NewLine} {message}");
            return;
        }
        
        await _decorated.HandleAsync(message, cancellationToken);
    }
}