using Microsoft.AspNetCore.Mvc;
using TicketFlow.CourseUtils;
using TicketFlow.Services.Inquiries.Core;
using TicketFlow.Services.Inquiries.Core.Commands.SubmitInquiry;
using TicketFlow.Services.Inquiries.Core.Commands.SubmitInquirySynchronously;
using TicketFlow.Services.Inquiries.Core.Queries;
using TicketFlow.Shared.AnomalyGeneration.HttpApi;
using TicketFlow.Shared.Metrics;
using TicketFlow.Shared.AspNetCore;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Queries;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddCore(builder.Configuration)
    .AddApiForFrontendConfigured();

var app = builder.Build();
app.UseMetrics();
app.ExposeApiForFrontend();
app.UseAnomalyEndpoints();

app.MapGet("/", () => "Inquiries Service");
app.MapGet("/instance", () => new { Service = "Inquiries", Instance = Environment.MachineName });

app.MapGet("/test-error", () => Results.StatusCode(500));

app.MapGet("/inquiries", async (
    [FromQuery] int page,
    [FromQuery] int limit,
    [FromHeader(Name = "X-User-Id")] Guid? userId,
    [FromServices] IQueryHandler<ListInquiries, InquiriesListDto> handler,
    CancellationToken cancellationToken)
    => Results.Ok((object?)await handler.HandleAsync(new(page, limit, userId), cancellationToken)));

app.MapPost("/inquiries/submit", async (
    [FromBody] SubmitInquiry command,
    [FromHeader(Name = "X-User-Id")] Guid? userId,
    [FromServices] ICommandHandler<SubmitInquirySynchronously> synchronousHandler,
    [FromServices] ICommandHandler<SubmitInquiry> handler,
    CancellationToken cancellationToken) =>
{
    // Use userId from header, override if present in command
    var effectiveUserId = userId ?? command.UserId;

    if (FeatureFlags.UseSynchronousIntegration)
    {
        var synchronousCommand = new SubmitInquirySynchronously(effectiveUserId, command.Name, command.Email, command.Title, command.Description, command.Category);
        await synchronousHandler.HandleAsync(synchronousCommand);
        return Results.Ok();
    }
    else
    {
        var commandWithUserId = command with { UserId = effectiveUserId };
        await handler.HandleAsync(commandWithUserId, cancellationToken);
        return Results.Ok();
    }
});

app.MapPost("/inquiries/submit-sync", async ([FromBody] SubmitInquirySynchronously command, [FromServices] ICommandHandler<SubmitInquirySynchronously> handler,
    CancellationToken cancellationToken) =>
{
    await handler.HandleAsync(command, cancellationToken);
    return Results.Ok();
});

app.Run();

public partial class Program { }