using TicketFlow.Services.BillingIntegration.Core.Repositories;

namespace TicketFlow.Services.BillingIntegration.Api;

public static class BillingEndpoints
{
    public static void MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var invoicesGroup = app.MapGroup("/invoices")
            .WithTags("Invoices");

        invoicesGroup.MapGet("/{invoiceId}", GetInvoice)
            .WithName("GetInvoice");

        var customersGroup = app.MapGroup("/customers")
            .WithTags("Customers");

        customersGroup.MapGet("/{domain}/payment-standing", GetPaymentStanding)
            .WithName("GetPaymentStanding");
    }

    private static async Task<IResult> GetInvoice(
        string invoiceId,
        IInvoiceRepository invoiceRepository,
        CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);

            if (invoice is null)
            {
                return Results.NotFound(new { error = $"Invoice {invoiceId} not found" });
            }

            return Results.Ok(invoice);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Failed to fetch invoice",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetPaymentStanding(
        string domain,
        IPaymentStandingRepository paymentStandingRepository,
        CancellationToken cancellationToken)
    {
        try
        {
            var standing = await paymentStandingRepository.GetByDomainAsync(domain, cancellationToken);
            return Results.Ok(standing);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Failed to check payment standing",
                detail: ex.Message,
                statusCode: 500);
        }
    }
}
