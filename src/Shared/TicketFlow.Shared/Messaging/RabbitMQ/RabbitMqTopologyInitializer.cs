using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Shared.Messaging.RabbitMQ;

internal sealed class RabbitMqTopologyInitializer : BackgroundService
{
    private readonly ITopologyBuilder _topologyBuilder;
    private readonly IOptions<RabbitMqOptions> _options;
    private readonly IMessageConsumerConventionProvider _messageConventionProvider;
    private readonly ILogger<RabbitMqTopologyInitializer> _logger;
    private readonly TopologyReadinessAccessor _topologyReadinessAccessor;

    public RabbitMqTopologyInitializer(
        ITopologyBuilder topologyBuilder, 
        IOptions<RabbitMqOptions> options,
        IMessageConsumerConventionProvider messageConventionProvider,
        ILogger<RabbitMqTopologyInitializer> logger,
        TopologyReadinessAccessor topologyReadinessAccessor)
    {
        _topologyBuilder = topologyBuilder;
        _options = options;
        _messageConventionProvider = messageConventionProvider;
        _logger = logger;
        _topologyReadinessAccessor = topologyReadinessAccessor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.Value.CreateTopology is false)
        {
            _logger.LogInformation("RabbitMq topology creation disabled");
            return;
        }
        if (_messageConventionProvider is not RabbitMqDefaultMessageConventionProvider)
        {
            _logger.LogInformation("RabbitMq topology not set due to custom convention provider.");
            return;
        }

        var messageTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x is {IsClass: true, IsAbstract: false} && typeof(IMessage).IsAssignableFrom(x))
            .Where(x => x.Namespace.Contains("Consuming"));

        _topologyReadinessAccessor.MarkTopologyProvisioningStart(GetType().Name);
        
        foreach (var messageType in messageTypes)
        {
            var (publisherDestination, _) = ((string, string)) typeof(IMessagePublisherConventionProvider)
                .GetMethod(nameof(IMessagePublisherConventionProvider.Get))
                .MakeGenericMethod(messageType)
                .Invoke(_messageConventionProvider, null);
            
            var (consumerDestination, _) = ((string, string)) typeof(IMessageConsumerConventionProvider)
                .GetMethod(nameof(IMessageConsumerConventionProvider.Get))
                .MakeGenericMethod(messageType)
                .Invoke(_messageConventionProvider, null);

            await _topologyBuilder.CreateTopologyAsync(publisherDestination, consumerDestination, TopologyType.Direct);
        }
        
        _topologyReadinessAccessor.MarkTopologyProvisioningEnd(GetType().Name);
    }
}