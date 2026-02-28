using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace TicketFlow.Shared.Messaging.Resiliency;

public class ReliableConsuming
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Guid, int> _messageIdAttemptsMade = new();
    private readonly bool _enabled;
    private readonly int _maxRetries;
    private readonly bool _produceFaults;

    public ReliableConsuming(ResiliencyOptions options, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _maxRetries = options.Consumer.BrokerRetriesLimit;
        _enabled = options.Consumer.BrokerRetriesEnabled;
        _produceFaults = options.Consumer.PublishFaultOnFailure;
    }

    public bool CanBrokerRetry(Guid messageId)
    {
        if (_enabled is false)
        {
            return true;
        }
        
        if (!_messageIdAttemptsMade.TryGetValue(messageId, out int consumeAttempts))
        {
            _messageIdAttemptsMade.TryAdd(messageId, 0);
            consumeAttempts = 0;
        }
        
        return consumeAttempts < _maxRetries + 1;
    }

    public void OnConsumeFailed(Guid messageId)
    {
        if (_enabled is false)
        {
            return;
        }
        
        if (!_messageIdAttemptsMade.TryGetValue(messageId, out int consumeAttempts))
        {
            _messageIdAttemptsMade.TryAdd(messageId, 0);
            consumeAttempts = 0;
        }
        
        _messageIdAttemptsMade.TryUpdate(messageId, consumeAttempts + 1, consumeAttempts);
    }

    public async Task OnBrokerRetriesExhausted<TMessage>(TMessage message, Exception exception, string failedQueue)
    {
        if (_produceFaults is false)
        {
            return;
        }
        
        var faultMessage = new Fault<TMessage>
        (
            message,
            exception.GetType().Name,
            exception.Message,
            failedQueue
        );

        var iocScope = _serviceProvider.CreateScope();
        var publisher = iocScope.ServiceProvider.GetService(typeof(IMessagePublisher)) as IMessagePublisher;
        
        await publisher!.PublishAsync(faultMessage);
    }
    
    public async Task OnBrokerRetriesExhaustedNonGeneric(string message, Exception exception, string failedQueue)
    {
        if (_produceFaults is false)
        {
            return;
        }
        
        var faultMessage = new Fault
        (
            message,
            exception.GetType().Name,
            exception.Message,
            failedQueue
        );

        var iocScope = _serviceProvider.CreateScope();
        var publisher = iocScope.ServiceProvider.GetService(typeof(IMessagePublisher)) as IMessagePublisher;
        
        await publisher!.PublishAsync(faultMessage);
    }
}