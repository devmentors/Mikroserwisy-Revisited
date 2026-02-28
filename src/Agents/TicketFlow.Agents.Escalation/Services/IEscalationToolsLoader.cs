using Microsoft.Extensions.AI;

namespace TicketFlow.Agents.Escalation.Services;

public interface IEscalationToolsLoader
{
    Task<List<AITool>> LoadToolsAsync(CancellationToken ct = default);
}
