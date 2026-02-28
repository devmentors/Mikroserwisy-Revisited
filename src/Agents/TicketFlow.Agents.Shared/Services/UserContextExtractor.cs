using Microsoft.AspNetCore.Http;
using TicketFlow.Agents.Shared.Models;

namespace TicketFlow.Agents.Shared.Services;

internal sealed class UserContextExtractor : IUserContextExtractor
{
    public UserContext Extract(HttpContext context) => Extract(context.Request);

    public UserContext Extract(HttpRequest request)
    {
        var userIdHeader = request.Headers["X-User-Id"].FirstOrDefault();
        var userId = userIdHeader?.Split(',')[0].Trim();

        var roleHeader = request.Headers["X-User-Role"].FirstOrDefault() ?? "client";
        var role = roleHeader.Split(',')[0].Trim().ToLowerInvariant();

        var emailHeader = request.Headers["X-User-Email"].FirstOrDefault() ?? "unknown@example.com";
        var email = emailHeader.Split(',')[0].Trim();

        var agentIdHeader = request.Headers["X-Agent-Id"].FirstOrDefault();
        var agentId = agentIdHeader?.Split(',')[0].Trim();

        return new UserContext
        {
            UserId = string.IsNullOrEmpty(userId) ? null : userId,
            Role = role,
            Email = email,
            AgentId = string.IsNullOrEmpty(agentId) ? null : agentId
        };
    }
}
