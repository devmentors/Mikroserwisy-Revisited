using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Tickets.Core.Queries.ListAgents;

public record ListAgentsQuery() : IQuery<AgentDto[]>;