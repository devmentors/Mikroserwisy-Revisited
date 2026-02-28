using TicketFlow.Services.Tickets.Core.Data.Models;

namespace TicketFlow.Services.SLA.Core.Data.Models;

public static class Defaults
{
    public static SLADeadlines Deadlines => new(new Dictionary<SeverityLevel, TimeSpan>
    {
        { SeverityLevel.Low, TimeSpan.FromDays(7) },
        { SeverityLevel.Medium, TimeSpan.FromDays(3) },
        { SeverityLevel.High, TimeSpan.FromDays(1) },
        { SeverityLevel.Critical, TimeSpan.FromHours(1) }
    });

    public static SignedSLA SLA => new(
        default,
        default,
        SLATier.Standard,
        new Dictionary<ServiceType, SLADeadlines>
        {
            {
                ServiceType.IncidentTicket,
                Deadlines
            },
            {
                ServiceType.QuestionTicket,
                Deadlines
            }
        });
}