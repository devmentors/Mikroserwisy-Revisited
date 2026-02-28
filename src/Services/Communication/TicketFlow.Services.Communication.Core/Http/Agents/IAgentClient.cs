namespace TicketFlow.Services.Communication.Core.Http.Agents;

public interface IAgentClient
{
    Task<AgentContactDto?> GetAgentAsync(string usedId, CancellationToken ct = default);
}
