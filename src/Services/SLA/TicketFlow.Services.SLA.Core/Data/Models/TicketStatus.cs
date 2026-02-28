namespace TicketFlow.Services.SLA.Core.Data.Models;

public static class TicketStatus
{
    public static bool IsFinishingForTicket(this string status)
    {
        return status.ToLower().Equals("resolved");
    }
    
    public const string Qualified = "Qualified";
    public const string AgentAssigned = "AgentAssigned";
    public const string Resolved = "Resolved";
}