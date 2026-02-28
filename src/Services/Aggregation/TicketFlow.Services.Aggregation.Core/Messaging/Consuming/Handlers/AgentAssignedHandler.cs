using Microsoft.Extensions.Logging;
using TicketFlow.Services.Aggregation.Core.Http;
using TicketFlow.Services.Aggregation.Core.Repositories;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Aggregation.Core.Messaging.Consuming.Handlers;

public class AgentAssignedHandler(
    ITicketProjectionRepository repository,
    ITicketsClient ticketsClient,
    ILogger<AgentAssignedHandler> logger) : IMessageHandler<AgentAssignedToTicket>
{
    public async Task HandleAsync(AgentAssignedToTicket message, CancellationToken cancellationToken = default)
    {
        var projection = await repository.GetByIdAsync(message.TicketId, cancellationToken);
        if (projection is null)
        {
            logger.LogWarning("TicketProjection {TicketId} not found for AgentAssigned", message.TicketId);
            return;
        }

        if (message.Version <= projection.Version)
        {
            logger.LogDebug("Skipping stale AgentAssigned (v{MessageVersion} <= v{ProjectionVersion})",
                message.Version, projection.Version);
            return;
        }

        try
        {
            var ticket = await ticketsClient.GetTicketAsync(message.TicketId, cancellationToken);
            if (ticket?.AssignedAgentUserId.HasValue == true)
            {
                var agent = await ticketsClient.GetAgentAsync(ticket.AssignedAgentUserId.Value, cancellationToken);
                if (agent is not null)
                {
                    projection.AgentId = ticket.AssignedAgentUserId;
                    projection.AgentName = agent.FullName;
                    projection.AgentAvatarUrl = agent.AvatarUrl;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch agent info for ticket {TicketId}", message.TicketId);
        }

        projection.Status = "Assigned";
        projection.Version = message.Version;
        await repository.UpdateAsync(projection, cancellationToken);
        logger.LogInformation("Agent assigned to ticket {TicketId}", message.TicketId);
    }
}
