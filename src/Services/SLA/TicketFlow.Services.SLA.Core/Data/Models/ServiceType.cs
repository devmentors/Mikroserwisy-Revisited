namespace TicketFlow.Services.SLA.Core.Data.Models;

public enum ServiceType : byte
{
    Unknown = 0,
    IncidentTicket = 1,
    QuestionTicket = 2
}

public static class ServiceTypeExtensions
{
    public static ServiceType? ParseAsServiceType(this string ticketType)
    {
        return ticketType switch
        {
            "Incident" => ServiceType.IncidentTicket,
            "Question" => ServiceType.QuestionTicket,
            _ => default(ServiceType?)
        };
    }

    public static string ToHumanReadableString(this ServiceType serviceType)
    {
        return serviceType switch
        {
            ServiceType.IncidentTicket => "Ticket - Incydent",
            ServiceType.QuestionTicket => "Ticket - Pytanie",
            _ => throw new NotImplementedException(serviceType.ToString()),
        };
    }
}