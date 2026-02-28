using Microsoft.Extensions.Logging;
using TicketFlow.Services.Tickets.Core.Services;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.Messaging.Consuming.Rebalance;

/// <summary>
/// Handles TicketStatusChanged events to trigger automatic rebalancing.
/// Has its own dedicated queue to avoid stealing events from other consumers.
/// </summary>
internal sealed class TicketStatusChangedHandler : IMessageHandler<TicketStatusChanged>
{
    private readonly RebalanceService _rebalanceService;
    private readonly ILogger<TicketStatusChangedHandler> _logger;

    public TicketStatusChangedHandler(
        RebalanceService rebalanceService,
        ILogger<TicketStatusChangedHandler> logger)
    {
        _rebalanceService = rebalanceService;
        _logger = logger;
    }

    public async Task HandleAsync(TicketStatusChanged message, CancellationToken cancellationToken = default)
    {
        // Only react to tickets being resolved (capacity freed up)
        if (!message.NewStatus?.Equals("Resolved", StringComparison.OrdinalIgnoreCase) == true)
        {
            return;
        }

        _logger.LogInformation(
            "Ticket {TicketId} resolved by agent {AgentId} - triggering rebalance check",
            message.TicketId, message.AgentId);

        await _rebalanceService.RebalanceAsync(cancellationToken);
    }
}
