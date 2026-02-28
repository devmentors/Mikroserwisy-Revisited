namespace TicketFlow.Services.Communication.Core.Data.Models;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? RecipentEmail { get; set; }
    public Guid? RecipentUserId { get; set; }
    public Guid? SenderUserId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public bool IsRead { get; set; }
}