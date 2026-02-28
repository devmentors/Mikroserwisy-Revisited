namespace TicketFlow.Services.Tickets.Core.Data.Models;

public class Agent
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string FullName { get; private set; }
    public AgentPosition JobPosition { get; private set; }
    public string AvatarUrl { get; private set; }
    public ICollection<Ticket> Tickets { get; }

    private Agent()
    {
    }

    public Agent(Guid id, Guid userId, string fullName, AgentPosition jobPosition, string avatarUrl)
    {
        Id = id;
        UserId = userId;
        FullName = fullName;
        JobPosition = jobPosition;
        AvatarUrl = avatarUrl;
    }
}