using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using TicketFlow.Shared.Messaging.Partitioning;
using TicketFlow.Shared.Messaging.Resiliency;
using TicketFlow.Shared.Messaging.Topology;

namespace TicketFlow.Shared.Messaging.RabbitMQ;

internal class RabbitMqTopologyBuilder(ChannelFactory channelFactory, ResiliencyOptions resiliencyOptions, ILogger<RabbitMqTopologyBuilder> logger) : ITopologyBuilder
{
    public const string PartitionKeyHeaderName = "hash-on";

    public Task CreateTopologyAsync(
        string publisherSource,
        string consumerDestination,
        TopologyType topologyType,
        string filter = "",
        Dictionary<string, object>? consumerCustomArgs = default,
        PartitioningOptions? partitioningOptions = default,
        CancellationToken cancellationToken = default)
    {
        var channel = channelFactory.CreateForConsumer();

        consumerCustomArgs ??= new Dictionary<string, object>();
        
        ConfigureDeadletter(consumerDestination, consumerCustomArgs, channel);
        
        switch (topologyType)
        {
            case TopologyType.Direct:
                CreateDirect(publisherSource, consumerDestination, filter, consumerCustomArgs, channel);
                break;
            case TopologyType.PublishSubscribe:
                if (partitioningOptions == null)
                {
                    CreatePubSub(publisherSource, consumerDestination, filter, consumerCustomArgs, channel);
                }
                else
                {
                    CreatePubSubWithPartitioning(publisherSource, consumerDestination, filter, consumerCustomArgs, channel, partitioningOptions);
                }
                break;
            case TopologyType.PublisherToPublisher:
                CreatePubToPub(publisherSource, consumerDestination, filter, channel);
                break;
            default:
                throw new NotImplementedException($"{nameof(topologyType)} is not supported!");
        }

        return Task.CompletedTask;
    }

    private void ConfigureDeadletter(string consumerDestination, Dictionary<string, object> consumerCustomArgs, IModel channel)
    {
        var dlqDeclaredByCaller = consumerCustomArgs.TryGetValue("x-dead-letter-exchange", out var dlqDeclared);
        var dlqExchange = dlqDeclared?.ToString();
        
        if (dlqDeclaredByCaller is false && resiliencyOptions.Consumer.UseDeadletter)
        {
            dlqExchange = consumerDestination + "-dlq-exchange";
            consumerCustomArgs.Add("x-dead-letter-exchange", dlqExchange);
        }

        // (Override) Caller explicitly set DLQ to be empty -> no DLQ
        if (dlqDeclaredByCaller && dlqExchange!.Equals(string.Empty))
        {
            consumerCustomArgs.Remove("x-dead-letter-exchange");
            return;
        }

        if (resiliencyOptions.Consumer.UseDeadletter && !string.IsNullOrEmpty(consumerDestination))
        {
            var dlqQueue = consumerDestination + "-dlq";
            CreatePubSub(dlqExchange, dlqQueue, "", null!, channel);
        }
    }

    private void CreateDirect(
        string publisherSource,
        string consumerDestination,
        string filter,
        Dictionary<string, object> consumerCustomArgs,
        IModel channel)
    {
        if (!string.IsNullOrEmpty(publisherSource))
        {
            logger.LogInformation($"Declaring exchange of name {publisherSource}");
            channel.ExchangeDeclare(publisherSource, ExchangeType.Direct, durable: true);
        }
        else
        {
            logger.LogInformation("Skipping publisher and binding due to direct publish to queue");
        }

        logger.LogInformation($"Declaring queue of name {consumerDestination}");
        channel.QueueDeclare(consumerDestination, durable: true, exclusive: false, autoDelete: false, consumerCustomArgs);

        if (!string.IsNullOrEmpty(publisherSource))
        {
            channel.QueueBind(queue: consumerDestination, exchange: publisherSource, routingKey: filter);
        }
    }

    private void CreatePubSub(
        string publisherSource,
        string consumerDestination,
        string filter,
        Dictionary<string, object> consumerCustomArgs,
        IModel channel)
    {
        logger.LogInformation($"Declaring exchange of name {publisherSource}");
        channel.ExchangeDeclare(publisherSource, ExchangeType.Topic, durable: true);

        if (string.IsNullOrEmpty(consumerDestination))
        {
            logger.LogInformation($"In {nameof(TopologyType.PublishSubscribe)} publisher is consumer-ignorant; skipping consumer creation and binding...");
            return;
        }
        
        logger.LogInformation($"Declaring queue of name {consumerDestination}");
        channel.QueueDeclare(consumerDestination, durable: true, exclusive: false, autoDelete: false, consumerCustomArgs);
        
        channel.QueueBind(queue: consumerDestination, exchange: publisherSource, 
            routingKey: string.IsNullOrEmpty(filter) 
                ? "#"       // Broadcast like fanout
                : filter    // custom filter pattern with substitute chars ('#' or '*')
        );
    }

    private void CreatePubSubWithPartitioning(
        string publisherSource,
        string consumerDestination,
        string filter,
        Dictionary<string, object> consumerCustomArgs,
        IModel channel,
        PartitioningOptions? partitioningOptions = default)
    {
        logger.LogInformation($"Declaring exchange of name {publisherSource}");
        channel.ExchangeDeclare(publisherSource, ExchangeType.Topic, durable: true);

        if (string.IsNullOrEmpty(consumerDestination))
        {
            logger.LogInformation($"In {nameof(TopologyType.PublishSubscribe)} publisher is consumer-ignorant; skipping consumer creation and binding...");
            return;
        }
        
        logger.LogInformation($"Requested partitioned topology for {consumerDestination}");
        var partitionedExchange = PartitionName.ForConsumerDedicatedExchange(consumerDestination);
        logger.LogInformation($"Declaring dedicated exchange for consumer with name {consumerDestination}");
        CreatePubToPub(publisherSource, partitionedExchange, filter, channel, forPartitioning: true);

        var consumerCustomArgsForPartitioning = new Dictionary<string, object>(consumerCustomArgs);
        consumerCustomArgsForPartitioning.Add("x-single-active-consumer", partitioningOptions.OnlyOneActiveConsumerPerPartition);
            
        logger.LogInformation($"Requested {partitioningOptions.NumberOfPartitions} partitions for {consumerDestination}");
            
        foreach (var partitionNum in Enumerable.Range(1, partitioningOptions.NumberOfPartitions))
        {
            var partitionedQueueName = PartitionName.ForQueue(consumerDestination, partitionNum);
            logger.LogInformation($"Declaring queue of name {partitionedQueueName}");
                
            channel.QueueDeclare(partitionedQueueName, durable: true, exclusive: false, autoDelete: false, consumerCustomArgsForPartitioning);
            
            channel.QueueBind(queue: partitionedQueueName, exchange: partitionedExchange, 
                routingKey: "1" /* Let's assume that each consumer has same weight, so they get partitions split evenly */
            );
        }
    }

    private void CreatePubToPub(string publisherSource, string consumerDestination, string filter, IModel channel, bool forPartitioning = false)
    {
        logger.LogInformation($"Declaring exchange of name {publisherSource}");
        channel.ExchangeDeclare(publisherSource, ExchangeType.Topic, durable: true);
            
        logger.LogInformation($"Declaring exchange of name {consumerDestination}");
        if (!forPartitioning)
        {
            channel.ExchangeDeclare(consumerDestination, ExchangeType.Topic, durable: true);
        }
        else
        {
            channel.ExchangeDeclare(
                consumerDestination,
                "x-consistent-hash",
                durable: true,
                arguments: new Dictionary<string, object>
                {
                    {
                        "hash-header", PartitionKeyHeaderName
                    }
                });
        }

        channel.ExchangeBind(source: publisherSource, destination: consumerDestination, routingKey: string.IsNullOrEmpty(filter) 
                ? "#"       // Broadcast like fanout
                : filter    // custom filter pattern with substitute chars ('#' or '*')
        );
    }
}