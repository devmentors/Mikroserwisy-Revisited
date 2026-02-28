namespace TicketFlow.Services.BillingIntegration.Core.Models;

public record Invoice(
    string Id,
    string CustomerReference,
    string InvoiceNumber,
    decimal TotalAmount,
    string Currency,
    InvoiceStatus Status,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    DateTimeOffset? PaidDate,
    IReadOnlyList<InvoiceLineItem> LineItems);

public record InvoiceLineItem(
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal LineAmount);

public enum InvoiceStatus
{
    Unpaid,
    Paid,
    Overdue,
    Cancelled
}
