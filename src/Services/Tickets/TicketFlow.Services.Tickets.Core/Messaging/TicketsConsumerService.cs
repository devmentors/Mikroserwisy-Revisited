using Microsoft.Extensions.Hosting;
using TicketFlow.CourseUtils;
using TicketFlow.Services.Tickets.Core.Messaging.Consuming.AnonymizationRequested;
using TicketFlow.Services.Tickets.Core.Messaging.Consuming.DeadlinesCalculated;
using TicketFlow.Services.Tickets.Core.Messaging.Consuming.InquirySubmitted;
using TicketFlow.Services.Tickets.Core.Messaging.Consuming.Rebalance;
using TicketFlow.Services.Tickets.Core.Messaging.Consuming.TranslationCompleted;
using TicketFlow.Services.Tickets.Core.Messaging.Publishing;
using TicketFlow.Shared.AnomalyGeneration.MessagingApi;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.Messaging;

internal sealed class TicketsConsumerService(IMessageConsumer messageConsumer, AnomalySynchronizationConfigurator anomalyConfigurator) : BackgroundService
{
    public const string SLAChangesQueue = "tickets-sla-changes";
    public const string TicketCreatedQueue = "tickets-ticket-created";
    public const string AnonymizationQueue = "tickets-anonymization-queue";
    public const string InquirySubmittedQueue = "inquiry-submitted-tickets-queue";
    public const string TranslationCompletedQueue = "translation-completed-tickets-queue";
    public const string RebalanceQueue = "tickets-rebalance-status-changed";  // Own queue to avoid stealing events

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!CourseUtils.FeatureFlags.UseSharedContracts)
        {
            await messageConsumer.ConsumeMessage<InquirySubmitted>(queue: InquirySubmittedQueue, acceptedMessageTypes: null, cancellationToken: stoppingToken);
        }
        else
        {
            await messageConsumer.ConsumeMessage<Shared.Contracts.Inquiries.Events.InquirySubmitted>(queue: InquirySubmittedQueue, acceptedMessageTypes: null, cancellationToken: stoppingToken);
        }

        await messageConsumer.ConsumeMessage<TranslationCompleted>(queue: TranslationCompletedQueue, acceptedMessageTypes: null, cancellationToken: stoppingToken);
        await messageConsumer.ConsumeMessage<DeadlinesCalculated>(queue: SLAChangesQueue, acceptedMessageTypes: ["DeadlinesCalculated"], cancellationToken: stoppingToken);
        if (FeatureFlags.UseListenToYourselfExample)
        {
            await messageConsumer.ConsumeMessage<TicketCreated>(queue: TicketCreatedQueue, acceptedMessageTypes: ["TicketCreated"], cancellationToken: stoppingToken);
        }
        await messageConsumer.ConsumeMessage<AnonymizationRequested>(queue: AnonymizationQueue, acceptedMessageTypes: null, cancellationToken: stoppingToken);

        // Rebalance trigger - listens to ticket status changes
        await messageConsumer.ConsumeMessage<Consuming.Rebalance.TicketStatusChanged>(queue: RebalanceQueue, acceptedMessageTypes: null, cancellationToken: stoppingToken);

        await anomalyConfigurator.ConsumeAnomalyChanges();
    }
}