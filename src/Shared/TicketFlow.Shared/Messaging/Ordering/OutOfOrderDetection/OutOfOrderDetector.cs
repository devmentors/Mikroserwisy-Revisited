using Microsoft.Extensions.Logging;

namespace TicketFlow.Shared.Messaging.Ordering.OutOfOrderDetection;

public class OutOfOrderDetector
{
    private readonly Func<Type, object?> _currentVersionAccessorFactory;
    private readonly ILogger<OutOfOrderDetector> _logger;

    public OutOfOrderDetector(
        /* The only reason this is not IServiceProvider is so that you can test it easily */
        Func<Type, object?> currentVersionAccessorFactory,
        ILogger<OutOfOrderDetector> logger)
    {
        _currentVersionAccessorFactory = currentVersionAccessorFactory;
        _logger = logger;
    }
    
    public async Task<bool> Check<TMessage>(TMessage message) where TMessage : IMessage
    {
        if (message is not IVersionedMessage versionedMessage)
        {
            _logger.LogInformation("{MessageType} is not versioned, thus cannot be verified", typeof(TMessage));
            return false;
        }
        
        var currentVersion = await GetCurrentVersion(versionedMessage);

        if (currentVersion == null)
        {
            return false;
        }
        
        return currentVersion >= versionedMessage.Version;
    }

    private async Task<int?> GetCurrentVersion<TMessage>(TMessage message) where TMessage : IVersionedMessage
    {
        var getterType = typeof(IGetMessageRelatedEntityVersion<>).MakeGenericType(message.GetType());
        var getter = _currentVersionAccessorFactory(getterType);

        if (getter is null)
        {
            _logger.LogWarning("No predicate for type {MessageType} found; version will not be checked!", typeof(TMessage));
            return null;
        }
        
        return await (Task<int?>)
            getterType.GetMethod(nameof(IGetMessageRelatedEntityVersion<IVersionedMessage>.GetEntityVersionAsync))
                .Invoke(getter, new object[] { message, CancellationToken.None });
    }
}