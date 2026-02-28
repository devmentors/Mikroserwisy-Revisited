using TicketFlow.Services.Tickets.Core.Data.Models;

namespace TicketFlow.Services.SLA.Core.Data.Models;

public record SLADeadlines(Dictionary<SeverityLevel, TimeSpan> ResponseDeadlines);