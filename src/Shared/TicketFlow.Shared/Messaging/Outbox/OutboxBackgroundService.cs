using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketFlow.Shared.AnomalyGeneration.CodeApi;
using TicketFlow.Shared.Messaging.Outbox.Data;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.Shared.Messaging.Outbox;

public class OutboxBackgroundService(IServiceProvider serviceProvider, IOptions<OutboxOptions> options,
    ILogger<OutboxBackgroundService> logger) : BackgroundService
{
    private OutboxDbContext _dbContext;
    private IMessageOutbox _messageOutbox;
    private IMessagePublisher _brokerPublisher;
    private ISerializer _serializer;
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var scope = serviceProvider.CreateScope();
            _dbContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
            _messageOutbox = scope.ServiceProvider.GetRequiredService<IMessageOutbox>();
            _serializer = scope.ServiceProvider.GetRequiredService<ISerializer>();
            var publishers = scope.ServiceProvider.GetRequiredService<IEnumerable<IMessagePublisher>>();
            var outboxPublishChannel = scope.ServiceProvider.GetRequiredService<OutboxPublishChannel>();
            _brokerPublisher = publishers.FirstOrDefault(x => x.GetType() != typeof(OutboxMessagePublisher));

            ProcessOutboxChannelAsync(outboxPublishChannel, cancellationToken);
        
            while (cancellationToken.IsCancellationRequested is false)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            
                try
                {
                    var unprocessedMessages = await _messageOutbox.GetUnsentAsync(options.Value.BatchSize, cancellationToken);
                    if (unprocessedMessages.Count > 0)
                    {
                        logger.LogInformation("Found {unprocessedMessageCount} unprocessed outbox messages. Publishing...",
                            unprocessedMessages.Count);
                    }
                    foreach (var outboxMessage in unprocessedMessages)
                    {
                        await PublishOutboxMessageAsync(outboxMessage, cancellationToken);
                    }
                
                    await transaction.CommitAsync(cancellationToken);
                }
                catch(Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    Console.WriteLine("Failed to publish outbox messages.");
                }
            
                await Task.Delay(options.Value.IntervalMilliseconds, cancellationToken);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
       
    }

    private async void ProcessOutboxChannelAsync(OutboxPublishChannel outboxPublishChannel, CancellationToken cancellationToken)
    {
        await foreach (var outboxMessage in outboxPublishChannel.GetAsync(cancellationToken))
        {
            await PublishOutboxMessageAsync(outboxMessage, cancellationToken);
        }
    }

    private async Task PublishOutboxMessageAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        var messageType = Type.GetType(outboxMessage.MessageType);
        var deserializedMessage = _serializer.Deserialize(outboxMessage.SerializedMessage, messageType);
        
        await (Task)_brokerPublisher.GetType()
            .GetMethod(nameof(IMessagePublisher.PublishAsync))
            .MakeGenericMethod(messageType)
            .Invoke(_brokerPublisher, new[] { deserializedMessage,outboxMessage.Destination,
                outboxMessage.RoutingKey, outboxMessage.MessageId, outboxMessage.Headers, cancellationToken });
        
        await _messageOutbox.MarkAsProcessedAsync(outboxMessage, cancellationToken);
        logger.LogInformation("Outbox message of type {messageType} with id {messageId} marked as processed.",
            Type.GetType(outboxMessage.MessageType).Name, outboxMessage.Id);
    }
}