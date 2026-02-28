using TicketFlow.Services.Tickets.Core.Data.Models;
using TicketFlow.Shared.Exceptions;

namespace TicketFlow.Services.SLA.Core.Data.Models;

public class SignedSLA
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string CompanyName { get; init; }
    public string Domain { get; init; }

    private Dictionary<ServiceType, SLADeadlines> _agreedResponseDeadlines;
    public IReadOnlyDictionary<ServiceType, SLADeadlines> AgreedResponseDeadlines => _agreedResponseDeadlines;
    
    public SLATier ClientTier { get; init; }
    
    private SignedSLA()
    {
        
    }
    
    public SignedSLA(string companyName, string domain, SLATier clientTier, Dictionary<ServiceType, SLADeadlines> agreedDeadlines)
    {
        CompanyName = companyName;
        Domain = domain;
        ClientTier = clientTier;
        _agreedResponseDeadlines = agreedDeadlines ?? throw new ArgumentNullException(nameof(agreedDeadlines));
    }

    public CalculatedDeadline? CalculatedDeadlineFor(DateTimeOffset requestReceiveDateUtc, ServiceType serviceType, SeverityLevel severityLevel)
    {
        AgreedResponseDeadlines.TryGetValue(serviceType, out var deadlines);
        if (deadlines is null)
        {
            return default;
        }

        deadlines.ResponseDeadlines.TryGetValue(severityLevel, out var deadline);
        if (deadline.Equals(TimeSpan.Zero))
        {
            deadline = Defaults.Deadlines.ResponseDeadlines[severityLevel];
        }

        return new CalculatedDeadline(requestReceiveDateUtc.Add(deadline), ClientTier);
    }
}