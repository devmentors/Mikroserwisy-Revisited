using Microsoft.Extensions.Logging;
using TicketFlow.Services.SLA.Core.Messaging.Consuming.ApplicationServices;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.SLA.Core.Messaging.Consuming;

public class AgentAssignedToTicketHandler : IMessageHandler<AgentAssignedToTicket>
{
    private readonly TicketService _ticketService;
    private readonly ILogger<TicketQualifiedHandler> _logger;

    public AgentAssignedToTicketHandler(        
        TicketService ticketService,
        ILogger<TicketQualifiedHandler> logger)
    {
        _ticketService = ticketService;
        _logger = logger;
    }
    
    public async Task HandleAsync(AgentAssignedToTicket message, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning($"[{DateTime.UtcNow:O}] {nameof(AgentAssignedToTicketHandler)} is processing:{Environment.NewLine} {message}");
        await _ticketService.HandleAgentAssignedAsync(message.TicketId, message.Version, cancellationToken);
    }
}