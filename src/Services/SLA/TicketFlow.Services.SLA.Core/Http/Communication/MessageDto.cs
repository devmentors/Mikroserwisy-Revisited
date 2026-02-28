namespace TicketFlow.Services.SLA.Core.Http.Communication;

public class MessageDto
{
    public string RecipentEmail { get; set; }
    public Guid? RecipentUserId { get; set; }
    public Guid? SenderUserId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}