using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.ClientsAPI.Contracts.V2;
using TicketFlow.ClientsAPI.Http;

namespace TicketFlow.ClientsAPI.Api;

public static class InquiriesEndpointsV2
{
    public static IEndpointRouteBuilder MapInquiriesV2(this IEndpointRouteBuilder app)
    {
        var v2 = app.NewVersionedApi()
            .MapGroup("/v2/inquiries")
            .HasApiVersion(2, 0);

        v2.MapPost("/", SubmitInquiry)
            .WithName("SubmitInquiryV2")
            .WithSummary("Submit a new inquiry (V2)")
            .WithDescription(ApiDescriptions.V2.SubmitInquiry)
            .Produces<InquiryCreatedV2Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        v2.MapGet("/{id:guid}", GetInquiry)
            .WithName("GetInquiryV2")
            .WithSummary("Get inquiry by ID (V2)")
            .WithDescription(ApiDescriptions.V2.GetInquiry)
            .Produces<InquiryV2Dto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        v2.MapGet("/", ListInquiries)
            .WithName("ListInquiriesV2")
            .WithSummary("List inquiries by customer email (V2)")
            .WithDescription(ApiDescriptions.V2.ListInquiries)
            .Produces<InquiriesListV2Response>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<IResult> SubmitInquiry(
        [FromBody] SubmitInquiryRequest request,
        IInquiriesClient client,
        CancellationToken cancellationToken)
    {
        var result = await client.SubmitAsync(request, cancellationToken);
        return Results.Created($"/v2/inquiries/{result.Id}", new InquiryCreatedV2Response(
            result.Id,
            "Inquiry submitted successfully",
            TimeSpan.FromHours(24)));
    }

    private static async Task<IResult> GetInquiry(
        Guid id,
        IInquiriesClient client,
        CancellationToken cancellationToken)
    {
        var inquiry = await client.GetByIdAsync(id, cancellationToken);
        if (inquiry is null)
            return Results.NotFound();

        return Results.Ok(new InquiryV2Dto(
            inquiry.Category,
            inquiry.CreatedAt,
            inquiry.Description,
            inquiry.Email,
            TimeSpan.FromHours(24),
            inquiry.Id,
            inquiry.Name,
            inquiry.Status.ToLowerInvariant(),
            "Standard",
            inquiry.TicketId,
            inquiry.Title));
    }

    private static async Task<IResult> ListInquiries(
        [FromQuery] string email,
        [FromQuery] int? page,
        [FromQuery] int? limit,
        IInquiriesClient client,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Results.BadRequest("Email parameter is required");

        var result = await client.GetByEmailAsync(email, limit ?? 100, cancellationToken);

        var items = result.Data.Select(i => new InquiryListItemV2Dto(
            i.Category,
            i.CreatedAt,
            i.Id,
            i.Status.ToLowerInvariant(),
            i.Title));

        var pagination = new PaginationV2Dto(page ?? 1, limit ?? 100, result.TotalCount);

        return Results.Ok(new InquiriesListV2Response(items, pagination));
    }
}
