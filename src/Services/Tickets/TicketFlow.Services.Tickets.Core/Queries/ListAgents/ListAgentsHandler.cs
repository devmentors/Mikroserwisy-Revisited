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
        return await _dbContext.Agents
            .Select(x =>
                new AgentDto(x.Id.ToString(), x.UserId.ToString(), x.FullName, x.JobPosition.ToString(), x.AvatarUrl))
            .ToArrayAsync(cancellationToken);
    }
}