using TicketFlow.Services.BillingIntegration.Core.Models;

namespace TicketFlow.Services.BillingIntegration.Core.Repositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(string invoiceId, CancellationToken cancellationToken = default);
}
