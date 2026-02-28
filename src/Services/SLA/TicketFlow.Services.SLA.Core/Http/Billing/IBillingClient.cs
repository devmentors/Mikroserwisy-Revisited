namespace TicketFlow.Services.SLA.Core.Http.Billing;

public interface IBillingClient
{
    Task<CustomerPaymentStandingDto?> GetPaymentStandingAsync(
        string domain, CancellationToken cancellationToken);
}
