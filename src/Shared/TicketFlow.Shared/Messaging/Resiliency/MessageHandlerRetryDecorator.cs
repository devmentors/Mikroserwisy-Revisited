using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace TicketFlow.Shared.Messaging.Resiliency;

internal sealed class MessageHandlerRetryDecorator<TMessage> : IMessageHandler<TMessage> where TMessage : class, IMessage
{
    private readonly RetryStrategyOptions _options;

    private readonly IMessageHandler<TMessage> _messageHandler;

    public MessageHandlerRetryDecorator(
        IMessageHandler<TMessage> messageHandler,
        ResiliencyOptions resiliencyOptions,
        ILogger<MessageHandlerRetryDecorator<TMessage>> logger)
    {
        _messageHandler = messageHandler;
        _options = new()
        {
            Delay = TimeSpan.FromSeconds(1),
            MaxRetryAttempts = resiliencyOptions.Consumer.ConsumerRetriesLimit,
            OnRetry = _ =>
            {
                if (_.AttemptNumber < resiliencyOptions.Consumer.ConsumerRetriesLimit - 1)
                {
                    logger.LogWarning("Consume failed - will retry via consumer (client-side)");
                }
                else
                {
                    logger.LogError("Consumer retries limit exhausted");
                }
                return ValueTask.CompletedTask;
            }
        };
    }

    public async Task HandleAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        var retryPolicy = new ResiliencePipelineBuilder()
            .AddRetry(_options)
            .Build();

        await retryPolicy.ExecuteAsync(async ct => await _messageHandler.HandleAsync(message, ct), cancellationToken);
    }
}