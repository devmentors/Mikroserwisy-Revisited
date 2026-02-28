using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.Communication.Api.DTO;
using TicketFlow.Services.Communication.Core;
using TicketFlow.Services.Communication.Core.Data;
using TicketFlow.Services.Communication.Core.Data.Models;
using TicketFlow.Services.Communication.Core.ExternalServices.Email;
using TicketFlow.Services.Communication.Core.Http.Agents;
using TicketFlow.Services.Communication.Core.Validators;
using TicketFlow.CourseUtils;
using TicketFlow.Services.Communication.Core.Messages;
using TicketFlow.Shared.AnomalyGeneration.HttpApi;
using TicketFlow.Shared.AspNetCore;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Metrics;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddCore(builder.Configuration)
    .AddApiForFrontendConfigured();

var app = builder.Build();

app.UseExceptions();
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
    [FromServices] IAgentClient agentClient,
    [FromQuery] bool onlyUnread = false,
    [FromQuery] int page = 1,
    [FromQuery] int limit = 10,
    CancellationToken cancellationToken = default) =>
{
    var dbQuery = dbContext.Messages
        .AsQueryable()
        .Where(x => x.RecipentUserId == null);

    if (onlyUnread)
    {
        dbQuery = dbQuery.Where(x => !x.IsRead);
    }

    var total = await dbQuery.CountAsync(cancellationToken);

    var messages = await dbQuery
        .OrderByDescending(x => x.Timestamp)
        .Skip((page - 1) * limit)
        .Take(limit)
        .ToListAsync(cancellationToken);

    var messagesWithSenders = new List<MessageWithSenderDto>();
    foreach (var msg in messages)
    {
        var senderDisplayName = "SYSTEM";
        if (msg.SenderUserId.HasValue)
        {
            var agent = await agentClient.GetAgentAsync(msg.SenderUserId.Value.ToString(), cancellationToken);
            if (agent is not null)
            {
                senderDisplayName = agent.DisplayName;
            }
        }

        var preview = msg.Content.Length > 100
            ? msg.Content[..100] + "..."
            : msg.Content;

        messagesWithSenders.Add(new MessageWithSenderDto(
            msg.Id,
            msg.RecipentEmail,
            msg.RecipentUserId,
            msg.SenderUserId,
            senderDisplayName,
            msg.Title,
            preview,
            msg.Content,
            msg.Timestamp,
            msg.IsRead));
    }

    return new MessageWithSenderListDto(messagesWithSenders, total);
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
    [FromServices] IMessageService messageService,
    [FromServices] IEmailService emailService,
    CancellationToken cancellationToken) =>
{
    new MessageValidator().Validate(message);

    await messageService.SaveMessageAsync(message, ct: cancellationToken);

    if (FeatureFlags.UseEmailNotifications)
    {
        var email = new EmailMessage(message.RecipentEmail, message.Title, message.Content, EmailPriority.Normal);
        var result = await emailService.SendAsync(email, cancellationToken);
        return Results.Ok(new { saved = true, emailSent = result.Success, emailError = result.ErrorMessage });
    }

    return Results.Ok(new { saved = true });
});

app.Run();