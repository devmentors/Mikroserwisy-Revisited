namespace TicketFlow.Services.SLA.Core.Http.Billing;

public record CustomerPaymentStandingDto(
    string Domain,
    bool InGoodStanding,
    int? DaysOverdue,
    decimal? OverdueAmount,
    int OverdueInvoiceCount);
