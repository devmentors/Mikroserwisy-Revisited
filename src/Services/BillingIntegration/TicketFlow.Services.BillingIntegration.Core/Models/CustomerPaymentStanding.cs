namespace TicketFlow.Services.BillingIntegration.Core.Models;

public record CustomerPaymentStanding(
    string Domain,
    bool InGoodStanding,
    int? DaysOverdue,
    decimal? OverdueAmount,
    int OverdueInvoiceCount);
