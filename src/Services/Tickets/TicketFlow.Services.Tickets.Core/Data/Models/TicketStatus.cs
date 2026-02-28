namespace TicketFlow.Services.Tickets.Core.Data.Models;

public enum TicketStatus : byte
{
    Unknown = 0,
    WaitingForScheduledAction = 1,
    BeforeQualification = 2,
    Qualified = 3,
    Resolved = 4,
    Blocked = 5,
    WaitingForCapacity = 6  // Ticket in queue waiting for agent capacity
}