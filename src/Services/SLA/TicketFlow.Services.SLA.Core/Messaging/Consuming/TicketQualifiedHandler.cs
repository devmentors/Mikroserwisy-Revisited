using Microsoft.Extensions.Logging;
using TicketFlow.Services.SLA.Core.Messaging.Consuming.ApplicationServices;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.SLA.Core.Messaging.Consuming;

public class TicketQualifiedHandler : IMessageHandler<TicketQualified>
{
    private readonly TicketService _ticketService;
    private readonly ILogger<TicketQualifiedHandler> _logger;

    public TicketQualifiedHandler(        
        TicketService ticketService,
        ILogger<TicketQualifiedHandler> logger)
    {
        _ticketService = ticketService;
        _logger = logger;
    }
    
    public async Task HandleAsync(TicketQualified message, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning($"[{DateTime.UtcNow:O}] {nameof(TicketQualifiedHandler)} is processing:{Environment.NewLine} {message}");
        await _ticketService.HandleTicketQualifiedAsync(message.TicketId, message.Version, cancellationToken);
    }
}