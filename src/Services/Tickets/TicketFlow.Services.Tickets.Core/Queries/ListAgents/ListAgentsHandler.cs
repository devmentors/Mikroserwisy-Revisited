using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.Tickets.Core.Data;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Tickets.Core.Queries.ListAgents;

internal class ListAgentsHandler : IQueryHandler<ListAgentsQuery, AgentDto[]>
{
    private readonly TicketsDbContext _dbContext;

    public ListAgentsHandler(TicketsDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<AgentDto[]> HandleAsync(ListAgentsQuery query, CancellationToken cancellationToken = default)
    {
        var agents = await _dbContext.Agents
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.FullName,
                x.JobPosition,
                x.AvatarUrl,
                AssignedTicketCount = x.Tickets.Count(t =>
                    t.Status != Data.Models.TicketStatus.Resolved &&
                    t.Status != Data.Models.TicketStatus.Blocked)
            })
            .ToArrayAsync(cancellationToken);

        return agents.Select(x => new AgentDto(
            Id: x.Id,
            UserId: x.UserId.ToString(),
            FullName: x.FullName,
            Position: x.JobPosition.ToString(),
            AvatarUrl: x.AvatarUrl,
            AssignedTicketCount: x.AssignedTicketCount,
            MaxTickets: GetMaxTicketsForPosition(x.JobPosition),
            Specializations: GetSpecializationsForPosition(x.JobPosition)
        )).ToArray();
    }

    private static int GetMaxTicketsForPosition(Data.Models.AgentPosition position)
    {
        return position switch
        {
            Data.Models.AgentPosition.Agent => 10,
            Data.Models.AgentPosition.Supervisor => 15,
            _ => 10
        };
    }

    private static string[] GetSpecializationsForPosition(Data.Models.AgentPosition position)
    {
        return position switch
        {
            Data.Models.AgentPosition.Agent => new[] { "General", "Technical", "Billing" },
            Data.Models.AgentPosition.Supervisor => new[] { "General", "Technical", "Billing", "Other" },
            _ => new[] { "Other" }
        };
    }
}