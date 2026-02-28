using Microsoft.AspNetCore.Mvc;
using TicketFlow.Services.PersonalInfoVault.Core;
using TicketFlow.Services.PersonalInfoVault.Core.Commands.AnonymizePersonalInfo;
using TicketFlow.Services.PersonalInfoVault.Core.Commands.StorePersonalInfo;
using TicketFlow.Services.PersonalInfoVault.Core.DTOs;
using TicketFlow.Services.PersonalInfoVault.Core.Queries;
using TicketFlow.Services.PersonalInfoVault.Core.Services;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Metrics;
using TicketFlow.Shared.Queries;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCore(builder.Configuration);

var app = builder.Build();
app.UseMetrics();

app.MapGet("/", () => "PersonalInfoVault Service");
app.MapGet("/instance", () => new { Service = "PersonalInfoVault", Instance = Environment.MachineName });

app.MapPost("/personal-info", async (
    [FromBody] StorePersonalInfoRequest request,
    [FromServices] ICommandHandler<StorePersonalInfo> handler,
    [FromServices] ITokenGenerator tokenGenerator,
    CancellationToken cancellationToken) =>
{
    var personToken = tokenGenerator.Generate();
    var command = new StorePersonalInfo(personToken, request.Name, request.Email);
    await handler.HandleAsync(command, cancellationToken);
    return Results.Ok(new StorePersonalInfoResponse(personToken));
});

app.MapGet("/personal-info/{personToken}", async (
    [FromRoute] string personToken,
    [FromServices] IQueryHandler<GetPersonalInfo, PersonalInfoDto?> handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(new GetPersonalInfo(personToken), cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapPost("/personal-info/{personToken}/anonymize", async (
    [FromRoute] string personToken,
    [FromServices] ICommandHandler<AnonymizePersonalInfo> handler,
    CancellationToken cancellationToken) =>
{
    await handler.HandleAsync(new AnonymizePersonalInfo(personToken), cancellationToken);
    return Results.Ok(new { Message = "Anonymized successfully" });
});

app.MapPost("/personal-info/batch", async (
    [FromBody] BatchRequest request,
    [FromServices] IQueryHandler<GetPersonalInfoBatch, BatchResponse> handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(new GetPersonalInfoBatch(request.Tokens), cancellationToken);
    return Results.Ok(result);
});

app.MapGet("/personal-info/by-email/{email}", async (
    [FromRoute] string email,
    [FromServices] IQueryHandler<GetPersonalInfoByEmail, PersonalInfoDto?> handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(new GetPersonalInfoByEmail(email), cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapGet("/personal-info/search", async (
    [FromQuery] string query,
    [FromQuery] int? limit,
    [FromServices] IQueryHandler<SearchPersonalInfo, SearchPersonalInfoDto> handler,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
    {
        return Results.BadRequest("Query must be at least 2 characters");
    }

    var result = await handler.HandleAsync(
        new SearchPersonalInfo(query, limit ?? 50),
        cancellationToken);
    return Results.Ok(result);
});

app.Run();
