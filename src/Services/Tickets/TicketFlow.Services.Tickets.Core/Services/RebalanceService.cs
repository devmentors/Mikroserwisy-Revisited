using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketFlow.Services.Tickets.Core.Data;
using TicketFlow.Services.Tickets.Core.Data.Models;
using TicketFlow.Services.Tickets.Core.Data.Repositories;

namespace TicketFlow.Services.Tickets.Core.Services;

/// <summary>
/// Provides deterministic rebalancing logic for ticket assignment.
/// Triggered by TicketStatusChanged events when tickets are resolved.
/// Uses direct database access (no MCP) for performance and simplicity.
/// </summary>
public class RebalanceService
{
    private readonly TicketsDbContext _dbContext;
    private readonly ITicketsRepository _repository;
    private readonly ILogger<RebalanceService> _logger;

    public RebalanceService(
        TicketsDbContext dbContext,
        ITicketsRepository repository,
        ILogger<RebalanceService> logger)
    {
        _dbContext = dbContext;
        _repository = repository;
        _logger = logger;
    }

    public async Task RebalanceAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting rebalance check");

        // PRIORITY 1: Check WaitingForCapacity queue
        var waitingTickets = await _dbContext.Tickets
            .Where(t => t.Status == TicketStatus.WaitingForCapacity)
            .OrderBy(t => t.CreatedAt)  // FIFO
            .ToListAsync(ct);

        if (waitingTickets.Any())
        {
            _logger.LogInformation("Found {Count} tickets in waiting queue", waitingTickets.Count);

            foreach (var ticket in waitingTickets)
            {
                var assigned = await TryAssignTicketAsync(ticket, ct);
                if (assigned)
                {
                    _logger.LogInformation(
                        "Assigned waiting ticket {TicketId} to agent {AgentId}",
                        ticket.Id, ticket.AssignedTo);
                }
            }

            // Update queue positions for remaining waiting tickets
            await _repository.UpdateQueuePositionsAsync(ct);
        }

        // PRIORITY 2: Fall back to unassigned tickets (existing logic)
        var unassignedTickets = await _dbContext.Tickets
            .Where(t => t.Status == TicketStatus.Qualified && t.AssignedTo == null)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);

        if (unassignedTickets.Any())
        {
            _logger.LogInformation("Found {Count} unassigned qualified tickets", unassignedTickets.Count);

            foreach (var ticket in unassignedTickets)
            {
                var assigned = await TryAssignTicketAsync(ticket, ct);
                if (assigned)
                {
                    _logger.LogInformation(
                        "Assigned unassigned ticket {TicketId} to agent {AgentId}",
                        ticket.Id, ticket.AssignedTo);
                }
            }
        }

        _logger.LogInformation("Rebalance check completed");
    }

    private async Task<bool> TryAssignTicketAsync(Ticket ticket, CancellationToken ct)
    {
        // Get specializations for ticket category
        var requiredSpecializations = GetSpecializationsForCategory(ticket.Category);

        // Find agents with matching specialization
        var agents = await _dbContext.Agents
            .Include(a => a.Tickets)
            .ToListAsync(ct);

        foreach (var agent in agents)
        {
            // Check if agent has matching specialization
            var agentSpecializations = GetSpecializationsForPosition(agent.JobPosition);
            var hasMatchingSpecialization = requiredSpecializations
                .Any(req => agentSpecializations.Contains(req, StringComparer.OrdinalIgnoreCase));

            if (!hasMatchingSpecialization)
            {
                continue;
            }

            // Count assigned tickets (excluding resolved and blocked)
            var assignedCount = agent.Tickets.Count(t =>
                t.Status != TicketStatus.Resolved &&
                t.Status != TicketStatus.Blocked);

            var maxTickets = GetMaxTicketsForPosition(agent.JobPosition);

            if (assignedCount < maxTickets)
            {
                // Capacity found! Assign ticket
                // AssignTo handles status transition from WaitingForCapacity -> Qualified and clears queue tracking
                ticket.AssignTo(agent.Id);
                ticket.AddInternalNote($"Automatically assigned to {agent.FullName} via rebalancing (capacity became available)");

                await _repository.UpdateAsync(ticket, ct);

                _logger.LogInformation(
                    "Assigned ticket {TicketId} to {AgentName} ({AssignedCount}/{MaxTickets})",
                    ticket.Id, agent.FullName, assignedCount + 1, maxTickets);

                // TODO: Publish TicketAssignedEvent to RabbitMQ for notifications

                return true;
            }
        }

        _logger.LogDebug(
            "Could not find agent with capacity for ticket {TicketId} (category: {Category})",
            ticket.Id, ticket.Category);

        return false;
    }

    private static int GetMaxTicketsForPosition(AgentPosition position)
    {
        return position switch
        {
            AgentPosition.Agent => 10,
            AgentPosition.Supervisor => 15,
            _ => 10
        };
    }

    private static string[] GetSpecializationsForPosition(AgentPosition position)
    {
        // Must match ListAgentsHandler.GetSpecializationsForPosition
        return position switch
        {
            AgentPosition.Agent => new[] { "Technical", "Billing", "AccountManagement" },
            AgentPosition.Supervisor => new[] { "Technical", "Billing", "AccountManagement", "FeatureRequest", "Other" },
            _ => new[] { "Other" }
        };
    }

    private static string[] GetSpecializationsForCategory(TicketCategory category)
    {
        // Map ticket categories to agent specializations
        return category switch
        {
            TicketCategory.Technical => new[] { "Technical" },
            TicketCategory.Billing => new[] { "Billing" },
            TicketCategory.General => new[] { "AccountManagement" },  // General maps to AccountManagement
            TicketCategory.Other => new[] { "Other" },
            _ => new[] { "Other" }
        };
    }
}
