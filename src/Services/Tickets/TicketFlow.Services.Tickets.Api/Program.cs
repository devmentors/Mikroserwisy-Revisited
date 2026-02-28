using Microsoft.AspNetCore.Mvc;
using TicketFlow.Services.Tickets.Api.DTO;
using TicketFlow.Services.Tickets.Core;
using TicketFlow.Services.Tickets.Core.Commands.AddClientCommentsToTicket;
using TicketFlow.Services.Tickets.Core.Commands.AssignAgentToTicket;
using TicketFlow.Services.Tickets.Core.Commands.BlockTicket;
using TicketFlow.Services.Tickets.Core.Commands.QualifyTicket;
using TicketFlow.Services.Tickets.Core.Commands.ResolveTicket;
using TicketFlow.Services.Tickets.Core.Commands.UnblockTicket;
using TicketFlow.Services.Tickets.Core.Data.Models;
using TicketFlow.Services.Tickets.Core.Queries.GetClientNotesForTicket;
using TicketFlow.Services.Tickets.Core.Queries.GetTicketDetails;
using TicketFlow.Services.Tickets.Core.Queries.ListAgents;
using TicketFlow.Services.Tickets.Core.Queries.SearchTicketsByPerson;
using TicketFlow.Services.Tickets.Core.Queries.ListTickets;
using TicketFlow.Services.Tickets.Core.SynchronousIntegration;
using TicketFlow.Shared.AnomalyGeneration.HttpApi;
using TicketFlow.Shared.AspNetCore;
using TicketFlow.Shared.Metrics;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Queries;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddCore(builder.Configuration)
    .AddApiForFrontendConfigured();

var app = builder.Build();

app.UseMetrics();
app.ExposeApiForFrontend();
app.UseAnomalyEndpoints();

app.MapGet("/", () => "Tickets Service");
app.MapGet("/instance", () => new { Service = "Tickets", Instance = Environment.MachineName });

app.MapGet("/tickets/",
    async (
        [FromServices] IQueryHandler<ListTicketsQuery, TicketsListDto> handler,
        CancellationToken cancellationToken,
        [FromQuery] Guid? agentId = default,
        [FromQuery] TicketStatus? status = default,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10) =>
    {
        var query = new ListTicketsQuery(agentId, status, page, limit);
        return await handler.HandleAsync(query, cancellationToken);
    });

app.MapGet("/tickets/{ticketId}/", async (
    [FromServices] IQueryHandler<GetTicketDetailsQuery, TicketDetailsDto> handler,
    CancellationToken cancellationToken,
    [FromRoute] Guid? ticketId) =>
{
    if (ticketId is null)
    {
        return Results.BadRequest();
    }
    var query = new GetTicketDetailsQuery(ticketId.Value);
    return Results.Ok(await handler.HandleAsync(query, cancellationToken));
});

app.MapPost("/tickets/",
    async (
        [FromBody] CreateTicketSynchronously command,
        [FromServices] ICommandHandler<CreateTicketSynchronously> handler,
        CancellationToken cancellationToken) =>
    {
        await handler.HandleAsync(command, cancellationToken);
        return Results.Created();
    });

app.MapPost("/tickets/{ticketId:guid}/qualify", async ([FromRoute] Guid ticketId, [FromBody] QualifyTicket input, 
    [FromServices] ICommandHandler<QualifyTicket> handler, CancellationToken cancellationToken) =>
{
    var command = input with {TicketId = ticketId};
    await handler.HandleAsync(command, cancellationToken);
    return Results.Ok();
});

app.MapPost("/tickets/{ticketId:guid}/assign/{agentId:guid}", async ([FromRoute] Guid ticketId, 
    [FromRoute] Guid agentId, [FromServices] ICommandHandler<AssignAgentToTicket> handler, CancellationToken cancellationToken) =>
{
    var command = new AssignAgentToTicket(ticketId, agentId);
    await handler.HandleAsync(command, cancellationToken);
    return Results.Ok();
});

app.MapPost("/tickets/{ticketId:guid}/block/{reason}", async ([FromRoute] Guid ticketId, [FromRoute] string reason,
    [FromServices] ICommandHandler<BlockTicket> handler, CancellationToken cancellationToken) =>
{
    var command = new BlockTicket(ticketId, reason);
    await handler.HandleAsync(command, cancellationToken);
    return Results.Ok();
});

app.MapPost("/tickets/{ticketId:guid}/unblock/{reason}", async ([FromRoute] Guid ticketId,[FromRoute] string reason,
    [FromServices] ICommandHandler<UnblockTicket> handler, CancellationToken cancellationToken) =>
{
    var command = new UnblockTicket(ticketId, reason);
    await handler.HandleAsync(command, cancellationToken);
    return Results.Ok();
});

app.MapPost("/tickets/{ticketId:guid}/resolve/{resolution}", async ([FromRoute] Guid ticketId, [FromRoute] string resolution,
    [FromServices] ICommandHandler<ResolveTicket> handler, CancellationToken cancellationToken) =>
{
    var command = new ResolveTicket(ticketId, resolution);
    await handler.HandleAsync(command, cancellationToken);
    return Results.Ok();
});

app.MapGet("/tickets/{ticketId}/client-notes", async (
    [FromRoute] Guid ticketId, 
    [FromServices] IQueryHandler<GetClientNotesForTicket, string> handler,
    CancellationToken cancellationToken) 
    => await handler.HandleAsync(new GetClientNotesForTicket(ticketId), cancellationToken));

app.MapPost("/tickets/{ticketId}/client-notes", async (
        [FromRoute] Guid ticketId,
        [FromBody] ClientNoteDto dto,
        [FromServices] ICommandHandler<AddClientNoteToTicket> handler,
        CancellationToken cancellationToken) 
    => await handler.HandleAsync(new AddClientNoteToTicket(ticketId, dto.Note), cancellationToken));

app.MapGet("/agents", async (
        [FromServices] IQueryHandler<ListAgentsQuery, AgentDto[]> handler,
        CancellationToken cancellationToken) =>
    await handler.HandleAsync(new ListAgentsQuery(), cancellationToken));

app.MapGet("/agents/{id}", async (
    [FromRoute] string id,
    [FromServices] IQueryHandler<ListAgentsQuery, AgentDto[]> handler,
    CancellationToken cancellationToken) =>
{
    //We have only 3 agents in system, so it's cheaper to just reuse fetching them all
    var allAgents = await handler.HandleAsync(new ListAgentsQuery(), cancellationToken);
    return allAgents.SingleOrDefault(x => x.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase));
});

app.MapGet("/users/{id}", async (
    [FromRoute] string id,
    [FromServices] IQueryHandler<ListAgentsQuery, AgentDto[]> handler,
    CancellationToken cancellationToken) =>
{
    //We have only 3 agents in system, so it's cheaper to just reuse fetching them all
    var allAgents = await handler.HandleAsync(new ListAgentsQuery(), cancellationToken);
    return allAgents.SingleOrDefault(x => x.UserId.Equals(id, StringComparison.InvariantCultureIgnoreCase));
});

app.MapPost("/tickets/by-tokens", async (
    [FromBody] FilterByTokensRequest request,
    [FromServices] IQueryHandler<SearchTicketsByPerson, TicketsListDto> handler,
    CancellationToken cancellationToken) =>
{
    if (request.Tokens is null || request.Tokens.Count == 0)
    {
        return Results.Ok(new TicketsListDto(new List<TicketsListEntryDto>(), 0));
    }

    var result = await handler.HandleAsync(new SearchTicketsByPerson(request.Tokens), cancellationToken);
    return Results.Ok(result);
});

app.UseExceptions();
app.Run();

public record FilterByTokensRequest(List<string> Tokens);