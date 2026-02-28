using Microsoft.Extensions.Logging;
using TicketFlow.Services.SLA.Core.Data.Models;
using TicketFlow.Services.SLA.Core.Messaging.Consuming.ApplicationServices;
using TicketFlow.Shared.Messaging;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.Services.SLA.Core.Messaging.Consuming.Demultiplexing;

public class TicketChangesHandler
{
    private readonly ISerializer _serializer;
    private readonly IMessageHandler<TicketQualified> _ticketQualifiedHandler;
    private readonly IMessageHandler<AgentAssignedToTicket> _agentAssignedToTicketHandler;
    private readonly IMessageHandler<TicketResolved> _ticketResolvedHandler;
    private readonly ILogger<TicketChangesHandler> _logger;

    public TicketChangesHandler(
        ISerializer serializer,
        IMessageHandler<TicketQualified> ticketQualifiedHandler,
        IMessageHandler<AgentAssignedToTicket> agentAssignedToTicketHandler,
        IMessageHandler<TicketResolved> ticketResolvedHandler,
        ILogger<TicketChangesHandler> logger)
    {
        _serializer = serializer;
        _ticketQualifiedHandler = ticketQualifiedHandler;
        _agentAssignedToTicketHandler = agentAssignedToTicketHandler;
        _ticketResolvedHandler = ticketResolvedHandler;
        _logger = logger;
    }

    public async Task HandleAsync(MessageData messageData, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning($"[{DateTime.UtcNow:O}] {nameof(TicketChangesHandler)} is processing:{Environment.NewLine} {messageData}");
        
        ITicketChange message = Demultiplex(messageData);
        
        _logger.LogWarning($"[{DateTime.UtcNow:O}] {nameof(TicketChangesHandler)} demultiplexed message data to:{Environment.NewLine} " +
                           $"{message}");
        
        switch (message)
        {
            case TicketQualified qualified:
                await _ticketQualifiedHandler.HandleAsync(qualified, cancellationToken);
                break;
            case AgentAssignedToTicket assigned:
                await _agentAssignedToTicketHandler.HandleAsync(assigned, cancellationToken);
                break;
            case TicketResolved resolved:
                await _ticketResolvedHandler.HandleAsync(resolved, cancellationToken);
                break;
            default:
                break;
        }
        
        _logger.LogWarning($"[{DateTime.UtcNow:O}] {nameof(TicketChangesHandler)} processed message:{Environment.NewLine} " +
                           $"{message}");
    }
    
    private ITicketChange Demultiplex(MessageData messageData)
    {
        switch (messageData.Type)
        {
            case "TicketQualified":
                return _serializer.DeserializeBinary<TicketQualified>(messageData.Payload);
            case "AgentAssignedToTicket":
                return _serializer.DeserializeBinary<AgentAssignedToTicket>(messageData.Payload);
            case "TicketResolved":
                return _serializer.DeserializeBinary<TicketResolved>(messageData.Payload);
            default:
                return _serializer.DeserializeBinary<FallbackTicketChangeEvent>(messageData.Payload);
        }
    }
}