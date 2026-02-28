namespace TicketFlow.Services.Communication.Core.Data.Models;

public class Alert
{
    public Guid Id { get; set; }
    public string Message { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public string Type { get; set; }
}