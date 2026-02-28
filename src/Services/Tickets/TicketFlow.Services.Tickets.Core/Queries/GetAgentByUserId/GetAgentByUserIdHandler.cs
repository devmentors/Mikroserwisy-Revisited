using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketFlow.Services.Tickets.Core.Data;
using TicketFlow.Shared.Caching;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Tickets.Core.Queries.GetAgentByUserId;

internal class GetAgentByUserIdHandler(
    TicketsDbContext dbContext,
    ICacheService cacheService,
    ILogger<GetAgentByUserIdHandler> logger) : IQueryHandler<GetAgentByUserId, AgentDetailsDto>
{
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(10);

    public async Task<AgentDetailsDto> HandleAsync(GetAgentByUserId query, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"agent:{query.UserId}";

        var cached = await cacheService.GetAsync<AgentDetailsDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation(
                "Agent cache hit for UserId {UserId}. Name: {FullName}, Position: {Position}",
                cached.UserId, cached.FullName, cached.Position);
            return cached;
        }

        logger.LogInformation("Agent cache miss for UserId {UserId}. Fetching from database...", query.UserId);

        var agent = await dbContext.Agents
            .Where(x => x.UserId.ToString() == query.UserId)
            .Select(x => new AgentDetailsDto(
                x.Id.ToString(),
                x.UserId.ToString(),
                x.FullName,
                x.JobPosition.ToString(),
                x.AvatarUrl))
            .FirstOrDefaultAsync(cancellationToken);

        if (agent is null)
        {
            logger.LogWarning("Agent with UserId {UserId} not found", query.UserId);
            return null!;
        }

        await cacheService.SetAsync(cacheKey, agent, CacheExpiry, cancellationToken);

        logger.LogInformation(
            "Agent {UserId} fetched and cached. Name: {FullName}, Position: {Position}",
            agent.UserId, agent.FullName, agent.Position);

        return agent;
    }
}
