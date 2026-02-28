using Microsoft.Extensions.Logging;
using TicketFlow.Services.SLA.Core.Messaging.Consuming.ApplicationServices;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.SLA.Core.Messaging.Consuming;

public class TicketResolvedHandler : IMessageHandler<TicketResolved>
{
    private readonly TicketService _ticketService;
    private readonly ILogger<TicketQualifiedHandler> _logger;

    public TicketResolvedHandler(        
        TicketService ticketService,
        ILogger<TicketQualifiedHandler> logger)
    {
        _ticketService = ticketService;
        _logger = logger;
    }
    
    public async Task HandleAsync(TicketResolved message, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning($"[{DateTime.UtcNow:O}] {nameof(TicketResolvedHandler)} is processing:{Environment.NewLine} {message}");
        await _ticketService.HandleTicketResolvedAsync(message.TicketId, message.Version, cancellationToken);
    }
}