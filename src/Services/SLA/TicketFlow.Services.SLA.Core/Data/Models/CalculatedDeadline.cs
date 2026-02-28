namespace TicketFlow.Services.SLA.Core.Data.Models;

public record CalculatedDeadline(DateTimeOffset DueDateUtc, SLATier ClientTier);