using Microsoft.AspNetCore.Mvc;
using TicketFlow.Services.Anonymization.Core;
using TicketFlow.Services.Anonymization.Core.Commands.CreateAnonymizationRequest;
using TicketFlow.Services.Anonymization.Core.DTOs;
using TicketFlow.Services.Anonymization.Core.Queries;
using TicketFlow.Shared.AspNetCore;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Metrics;
using TicketFlow.Shared.Queries;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddCore(builder.Configuration)
    .AddMetrics(builder.Configuration)
    .AddApiForFrontendConfigured();

var app = builder.Build();

app.UseExceptions();
app.UseMetrics();

app.MapGet("/", () => "Anonymization Service - GDPR Data Removal Orchestration");

app.MapGet("/anonymization-requests", async (
    [FromServices] IQueryHandler<ListAnonymizationRequests, List<AnonymizationRequestDto>> handler,
    CancellationToken cancellationToken) =>
{
    var requests = await handler.HandleAsync(new ListAnonymizationRequests(), cancellationToken);
    return Results.Ok(requests);
});

app.MapGet("/anonymization-requests/{id:guid}", async (
    [FromRoute] Guid id,
    [FromServices] IQueryHandler<GetAnonymizationRequest, AnonymizationRequestDto?> handler,
    CancellationToken cancellationToken) =>
{
    var request = await handler.HandleAsync(new GetAnonymizationRequest(id), cancellationToken);
    return request is null ? Results.NotFound() : Results.Ok(request);
});

app.MapPost("/anonymization-requests", async (
    [FromBody] CreateAnonymizationRequestInput input,
    [FromServices] ICommandHandler<CreateAnonymizationRequest> handler,
    CancellationToken cancellationToken) =>
{
    var requestId = Guid.NewGuid();
    var command = new CreateAnonymizationRequest(requestId, input.PersonToken, input.Email, input.RequestedByEmail);
    await handler.HandleAsync(command, cancellationToken);

    return Results.Created($"/anonymization-requests/{requestId}", new
    {
        Id = requestId,
        Status = "InProgress"
    });
});

app.ExposeApiForFrontend();

app.Run();
