using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.Communication.Api.DTO;
using TicketFlow.Services.Communication.Core;
using TicketFlow.Services.Communication.Core.Data;
using TicketFlow.Services.Communication.Core.Data.Models;
using TicketFlow.Services.Communication.Core.Validators;
using TicketFlow.Shared.AnomalyGeneration.HttpApi;
using TicketFlow.Shared.AspNetCore;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Metrics;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddCore(builder.Configuration)
    .AddApiForFrontendConfigured();

var app = builder.Build();

app.UseMetrics();
app.ExposeApiForFrontend();
app.UseAnomalyEndpoints();

app.MapGet("/alerts/", async (
    [FromServices] CommunicationDbContext dbContext,
    [FromQuery] bool onlyUnread = false,
    CancellationToken cancellationToken = default) =>
{
    var dbQuery = dbContext.Alerts
        .AsQueryable();
        
    if (onlyUnread)
    {
        dbQuery = dbQuery.Where(x => !x.IsRead);
    }
    
    return await dbQuery
        .OrderByDescending(x => x.CreatedAt)
        .Take(10)
        .ToListAsync(cancellationToken);
});

app.MapPut("/alerts/{alertId}", async (
    [FromRoute] Guid alertId,
    [FromQuery] bool isRead,
    [FromServices] CommunicationDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var alert = await dbContext.Alerts.SingleOrDefaultAsync(x => x.Id == alertId, cancellationToken);
    alert.IsRead = isRead;
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok();
});

app.MapGet("/logged-users/{userId}/messages/", async (
    [FromServices] CommunicationDbContext dbContext,
    [FromRoute] Guid userId,
    [FromQuery] bool onlyUnread = false,
    [FromQuery] int page = 1,
    [FromQuery] int limit = 10,
    CancellationToken cancellationToken = default) =>
{
    var dbQuery = dbContext.Messages
        .AsQueryable()
        .Where(x => x.RecipentUserId.Equals(userId));

    if (onlyUnread)
    {
        dbQuery = dbQuery.Where(x => !x.IsRead);
    }
    
    var total = await dbQuery.CountAsync(cancellationToken);
    
    var data = await dbQuery
        .OrderByDescending(x => x.Timestamp)
        .Skip((page - 1) * limit)
        .Take(limit)
        .ToListAsync(cancellationToken);
    
    return new MessageListDto(data, total);
});

app.MapGet("/anonymous-users/messages/", async (
    [FromServices] CommunicationDbContext dbContext,
    [FromHeader(Name = "X-User-Id")] Guid? userId,
    [FromQuery] bool onlyUnread = false,
    [FromQuery] int page = 1,
    [FromQuery] int limit = 10,
    CancellationToken cancellationToken = default) =>
{
    var dbQuery = dbContext.Messages.AsQueryable();

    if (userId.HasValue)
    {
        dbQuery = dbQuery.Where(x => x.RecipentUserId == userId.Value);
    }
    else
    {
        dbQuery = dbQuery.Where(x => x.RecipentUserId == null);
    }

    if (onlyUnread)
    {
        dbQuery = dbQuery.Where(x => !x.IsRead);
    }

    var total = await dbQuery.CountAsync(cancellationToken);

    var data = await dbQuery
        .OrderByDescending(x => x.Timestamp)
        .Skip((page - 1) * limit)
        .Take(limit)
        .ToListAsync(cancellationToken);

    return new MessageListDto(data, total);
});

app.MapPut("/messages/{messageId}", async (
    [FromRoute] Guid messageId,
    [FromQuery] bool isRead,
    [FromServices] CommunicationDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var message = await dbContext.Messages.SingleOrDefaultAsync(x => x.Id == messageId, cancellationToken);
    message.IsRead = isRead;
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok();
});

app.MapPost("/messages", async (
    [FromBody] Message message,
    [FromServices] CommunicationDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    new MessageValidator().Validate(message);

    dbContext.Messages.Add(message);
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok();
});

app.Run();