using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.ClientsAPI.Contracts.V1;
using TicketFlow.ClientsAPI.Http;

namespace TicketFlow.ClientsAPI.Api;

public static class InquiriesEndpointsV1
{
    public static IEndpointRouteBuilder MapInquiriesV1(this IEndpointRouteBuilder app)
    {
        var v1 = app.NewVersionedApi()
            .MapGroup("/v1/inquiries")
            .HasApiVersion(1, 0)
            .AddEndpointFilter(async (context, next) =>
            {
                context.HttpContext.Response.Headers["Deprecation"] = "true";
                context.HttpContext.Response.Headers["Sunset"] = "2026-12-31";
                context.HttpContext.Response.Headers["Link"] = "</v2/inquiries>; rel=\"successor-version\"";
                return await next(context);
            });

        v1.MapPost("/", SubmitInquiry)
            .WithName("SubmitInquiryV1")
            .WithSummary("Submit a new inquiry")
            .WithDescription(ApiDescriptions.V1.SubmitInquiry)
            .Produces<InquiryCreatedV1Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        v1.MapGet("/{id:guid}", GetInquiry)
            .WithName("GetInquiryV1")
            .WithSummary("Get inquiry by ID")
            .WithDescription(ApiDescriptions.V1.GetInquiry)
            .Produces<InquiryV1Dto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        v1.MapGet("/", ListInquiries)
            .WithName("ListInquiriesV1")
            .WithSummary("List inquiries by customer email")
            .WithDescription(ApiDescriptions.V1.ListInquiries)
            .Produces<InquiriesListV1Response>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<IResult> SubmitInquiry(
        [FromBody] SubmitInquiryRequest request,
        IInquiriesClient client,
        CancellationToken cancellationToken)
    {
        var result = await client.SubmitAsync(request, cancellationToken);
        return Results.Created($"/v1/inquiries/{result.Id}", new InquiryCreatedV1Response(result.Id, "Inquiry submitted successfully"));
    }

    private static async Task<IResult> GetInquiry(
        Guid id,
        IInquiriesClient client,
        CancellationToken cancellationToken)
    {
        var inquiry = await client.GetByIdAsync(id, cancellationToken);
        if (inquiry is null)
            return Results.NotFound();

        return Results.Json(new InquiryV1Dto(
            inquiry.Id,
            inquiry.Title,
            inquiry.Status,
            inquiry.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            inquiry.Name,
            inquiry.Email,
            inquiry.Description,
            inquiry.Category,
            inquiry.TicketId));
    }

    private static async Task<IResult> ListInquiries(
        [FromQuery] string email,
        [FromQuery] int? limit,
        IInquiriesClient client,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Results.BadRequest("Email parameter is required");

        var result = await client.GetByEmailAsync(email, limit ?? 100, cancellationToken);

        var items = result.Data.Select(i => new InquiryListItemV1Dto(
            i.Id,
            i.Title,
            i.Status,
            i.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")));

        return Results.Ok(new InquiriesListV1Response(items, result.TotalCount));
    }
}
