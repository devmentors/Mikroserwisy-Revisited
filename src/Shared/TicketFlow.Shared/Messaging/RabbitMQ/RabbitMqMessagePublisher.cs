using RabbitMQ.Client;
using TicketFlow.CourseUtils;
using TicketFlow.Shared.Messaging.Partitioning;
using TicketFlow.Shared.Messaging.Resiliency;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.Shared.Messaging.RabbitMQ;

internal sealed class RabbitMqMessagePublisher(ChannelFactory channelFactory, IMessagePublisherConventionProvider conventionProvider, 
    ISerializer serializer, MessagePropertiesAccessor propertiesAccessor, ReliablePublishing reliablePublishing) : IMessagePublisher
{
    public async Task PublishAsync<TMessage>(TMessage message, string? destination = default, string? routingKey = default, string? messageId = default,
        IDictionary<string, object>? headers = default, CancellationToken cancellationToken = default) where TMessage : class,IMessage
    {
        var channel = channelFactory.CreateForProducer();
        var payload = serializer.SerializeBinary(message);
        var properties = CreateMessageProperties<TMessage>(channel, messageId, headers);
        SetPartitionKey(properties, message);
        
        var (conventionDestination, conventionRoutingKey) = conventionProvider.Get<TMessage>();
        
        ConfigureReliablePublishing<TMessage>(channel, messageId);
        
        channel.BasicPublish(
            exchange: destination ?? conventionDestination,
            routingKey: routingKey ?? conventionRoutingKey,
            basicProperties: properties,
            body: payload,
            mandatory: reliablePublishing.ShouldPublishAsMandatory<TMessage>());

        EnsureReliablePublish(channel);
            
        await Task.CompletedTask;
    }

    private IBasicProperties CreateMessageProperties<TMessage>(IModel channel, string?  messageId = default, IDictionary<string, object>? headers = default)
        where TMessage : class,IMessage
    {
        var messageProperties = propertiesAccessor.Get();
        var basicProperties = channel.CreateBasicProperties();

        basicProperties.MessageId = messageId ?? Guid.NewGuid().ToString();
        basicProperties.Type = MessageTypeName.CreateFor<TMessage>();
        basicProperties.DeliveryMode = 2;
        basicProperties.Headers = new Dictionary<string, object>();
        
        var headersToAdd = headers 
                           ?? messageProperties?.Headers 
                           ?? Enumerable.Empty<KeyValuePair<string, object>>();

        foreach (var header in headersToAdd)
        {
            basicProperties.Headers.Add(header.Key, header.Value.ToString());
        }
        
        return basicProperties;
    }

    private void ConfigureReliablePublishing<TMessage>(IModel channel, string? messageId)
    {
        if (reliablePublishing.UsePublisherConfirms)
        {
            channel.ConfirmSelect();
        }

        if (reliablePublishing.ShouldPublishAsMandatory<TMessage>())
        {
            channel.BasicReturn += (s, args) =>
            {
                Console.WriteLine($"Message {typeof(TMessage).Name}, id: {messageId} was not routed properly to any consumer!)");
            };
        }
    }
    
    private void EnsureReliablePublish(IModel channel)
    {
        if (reliablePublishing.UsePublisherConfirms)
        {
            try
            {
                channel.WaitForConfirmsOrDie();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
    }

    private void SetPartitionKey<TMessage>(IBasicProperties basicProperties, TMessage? message)
    {
        if (FeatureFlags.UsePartitioningExample is false)
        {
            return;
        }

        if (typeof(IMessageWithPartitionKey).IsAssignableFrom(typeof(TMessage)))
        {
            var partitionKey = (message as IMessageWithPartitionKey)?.PartitionKey;
            basicProperties.Headers.Add(RabbitMqTopologyBuilder.PartitionKeyHeaderName, partitionKey);
        }
    }
}