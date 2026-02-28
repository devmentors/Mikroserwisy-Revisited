using TicketFlow.Services.BillingIntegration.Core.Models;

namespace TicketFlow.Services.BillingIntegration.Core.Repositories;

public interface IPaymentStandingRepository
{
    Task<CustomerPaymentStanding> GetByDomainAsync(string domain, CancellationToken cancellationToken = default);
}
