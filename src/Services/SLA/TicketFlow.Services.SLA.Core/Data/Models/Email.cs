namespace TicketFlow.Services.SLA.Core.Data.Models;

public record Email(string Value)
{
    public string Domain => Value.Split("@")[1];
}