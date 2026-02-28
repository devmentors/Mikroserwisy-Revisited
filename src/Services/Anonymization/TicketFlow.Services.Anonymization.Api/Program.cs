using Microsoft.AspNetCore.Mvc;
using TicketFlow.Services.Anonymization.Core;
using TicketFlow.Services.Anonymization.Core.Commands.CreateAnonymizationRequest;
using TicketFlow.Services.Anonymization.Core.DTOs;
using TicketFlow.Services.Anonymization.Core.Http;
using TicketFlow.Services.Anonymization.Core.Queries;
using TicketFlow.Shared.AspNetCore;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Metrics;
using TicketFlow.Shared.Queries;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddCore(builder.Configuration)
    .AddMetrics(builder.Configuration)
    .AddApiForFrontendConfigured();

var app = builder.Build();

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
    [FromServices] IPersonalInfoVaultClient vaultClient,
    CancellationToken cancellationToken) =>
{
    var personToken = input.PersonToken;

    // Resolve personToken from email if needed
    if (string.IsNullOrEmpty(personToken) && !string.IsNullOrEmpty(input.Email))
    {
        personToken = await vaultClient.GetPersonTokenByEmailAsync(input.Email, cancellationToken);
        if (string.IsNullOrEmpty(personToken))
        {
            return Results.NotFound(new { Message = "No person found with this email" });
        }
    }

    if (string.IsNullOrEmpty(personToken))
    {
        return Results.BadRequest(new { Message = "Either PersonToken or Email must be provided" });
    }

    var requestId = Guid.NewGuid();
    var command = new CreateAnonymizationRequest(requestId, personToken, input.RequestedByEmail);
    await handler.HandleAsync(command, cancellationToken);

    return Results.Created($"/anonymization-requests/{requestId}", new
    {
        Id = requestId,
        PersonToken = personToken,
        Status = "InProgress"
    });
});

app.ExposeApiForFrontend();

app.Run();
