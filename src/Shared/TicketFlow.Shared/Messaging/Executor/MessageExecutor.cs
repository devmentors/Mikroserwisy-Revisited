using System.Diagnostics;
using System.Transactions;
using Microsoft.Extensions.Logging;
using TicketFlow.Shared.AnomalyGeneration.CodeApi;
using TicketFlow.Shared.Observability;

namespace TicketFlow.Shared.Messaging.Executor;

internal sealed class MessageExecutor(
    IEnumerable<IMessageExecutionStep> executionSteps,
    MessagePropertiesAccessor messagePropertiesAccessor, 
    ILogger<MessageExecutor> logger,
    AnomalyContextAccessor anomalyContextAccessor) : IMessageExecutor
{
    public async Task ExecuteAsync(Func<Task> handle, CancellationToken cancellationToken)
    {
        var messageProperties = messagePropertiesAccessor.InitializeIfEmpty();
        anomalyContextAccessor.InitializeIfEmpty();
        using var activity = CreateMessagingExecutionActivity(messageProperties);

        try
        {
            await ExecuteStepsAsync(ExecutionType.BeforeTransaction, cancellationToken);

            using (var scope = BeginTransaction())
            {
                await handle();
                await ExecuteStepsAsync(ExecutionType.WithinTransaction, cancellationToken);

                scope.Complete();
                logger.LogInformation("Transactional processing of the message finished.");
            }

            await ExecuteStepsAsync(ExecutionType.AfterTransaction, cancellationToken);
        }
        catch (MessageExecutionAbortedException ex)
        {
            logger.LogWarning("Message execution aborted due to the: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError("Error occured during transactional processing of the message: {Error}. Rolling back.", ex.Message);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private async Task ExecuteStepsAsync(ExecutionType type, CancellationToken cancellationToken)
    {
        var messageProperties = messagePropertiesAccessor.Get();
        var steps = executionSteps.Where(x => x.Type == type);

        var pipeline = () => Task.CompletedTask;

        foreach (var step in steps.Reverse())
        {
            var nextStep = pipeline;
            pipeline = () => step.ExecuteAsync(messageProperties!, nextStep, cancellationToken);
        }

        await pipeline();
    }

    private TransactionScope BeginTransaction()
    {
        var opts = new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
        };
        return new TransactionScope(
            TransactionScopeOption.Required,
            opts,
            TransactionScopeAsyncFlowOption.Enabled);
    }

    private Activity? CreateMessagingExecutionActivity(MessageProperties messageProperties)
    {
        var activitySource = new ActivitySource(MessagingActivitySources.MessagingPublishSourceName);
        var activity = activitySource.StartActivity($"Message Execution: {messageProperties.MessageType}", ActivityKind.Producer, Activity.Current?.Context ?? default);

        if (activity is not null)
        {
            messageProperties.Headers.TryAdd(MessagingObservabilityHeaders.TraceParent, activity.Id);
        }

        return activity;
    }
}