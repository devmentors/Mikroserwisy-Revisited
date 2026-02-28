using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TicketFlow.Shared.App;

namespace TicketFlow.Shared.Messaging.RabbitMQ;

internal sealed class ChannelFactory(ConnectionProvider connectionProvider, IOptions<AppOptions> appOptions) : IDisposable
{
    private readonly ThreadLocal<IModel> _consumerCache = new(true);
    private readonly ThreadLocal<IModel> _producerCache = new(true);

    public IModel CreateForProducer() => Create(connectionProvider.ProducerConnection, _producerCache);
    
    public IModel CreateForConsumer() => Create(connectionProvider.ConsumerConnection, _consumerCache);
    
    private IModel Create(IConnection connection, ThreadLocal<IModel> cache)
    {
        if (cache.Value is not null)
        {
            return cache.Value;
        }
        
        var channel = connection.CreateModel();
        cache.Value = channel;
        return channel;
    }
    
    public void Dispose()
    {
        foreach (var channel in _consumerCache.Values)
        {
            channel.Dispose();
        }
        foreach (var channel in _producerCache.Values)
        {
            channel.Dispose();
        }
        
        _consumerCache.Dispose();
        _producerCache.Dispose();
    }
}
