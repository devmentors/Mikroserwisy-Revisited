using Microsoft.AspNetCore.Mvc;
using TicketFlow.Services.Aggregation.Core;
using TicketFlow.Services.Aggregation.Core.Http;
using TicketFlow.Services.Aggregation.Core.Models;
using TicketFlow.Services.Aggregation.Core.Repositories;
using TicketFlow.Shared.AspNetCore;
using TicketFlow.Shared.Metrics;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddCore(builder.Configuration)
    .AddMetrics(builder.Configuration)
    .AddApiForFrontendConfigured();

var app = builder.Build();

app.UseMetrics();

app.MapGet("/", () => "Aggregation Service - Event-Driven Projections");

app.MapGet("/projections", async (
    [FromServices] ITicketProjectionRepository repository,
    [FromServices] IPersonalInfoVaultClient personalInfoVaultClient,
    CancellationToken cancellationToken) =>
{
    var projections = await repository.GetAllAsync(cancellationToken);

    // Batch detokenization
    var tokens = projections.Select(p => p.PersonToken).Distinct().ToList();
    var personalInfos = await personalInfoVaultClient.GetBatchAsync(tokens, cancellationToken);
    var piiByToken = personalInfos.ToDictionary(p => p.PersonToken);

    var result = projections.Select(p =>
    {
        var pii = piiByToken.GetValueOrDefault(p.PersonToken);
        return new TicketProjectionDto(
            p.Id,
            p.InquiryId,
            pii?.Name ?? "Unknown",
            pii?.Email ?? "Unknown",
            p.Title,
            p.Description,
            p.Category,
            p.LanguageCode,
            p.Status,
            p.SeverityLevel,
            p.CreatedAt,
            p.UpdatedAt,
            p.AgentId,
            p.AgentName,
            p.AgentAvatarUrl,
            p.SlaDeadlineUtc,
            p.SlaBreached,
            p.SlaServiceCompleted,
            p.Version);
    }).ToList();

    return Results.Ok(result);
});

app.MapGet("/statistics", async (
    [FromServices] ITicketProjectionRepository repository,
    CancellationToken cancellationToken) =>
{
    var statistics = await repository.GetStatisticsAsync(cancellationToken);
    return Results.Ok(statistics);
});

app.MapGet("/projections/{id:guid}", async (
    [FromRoute] Guid id,
    [FromServices] ITicketProjectionRepository repository,
    [FromServices] IPersonalInfoVaultClient personalInfoVaultClient,
    CancellationToken cancellationToken) =>
{
    var projection = await repository.GetByIdAsync(id, cancellationToken);
    if (projection is null) return Results.NotFound();

    // Detokenization
    var pii = await personalInfoVaultClient.GetAsync(projection.PersonToken, cancellationToken);

    var result = new TicketProjectionDto(
        projection.Id,
        projection.InquiryId,
        pii?.Name ?? "Unknown",
        pii?.Email ?? "Unknown",
        projection.Title,
        projection.Description,
        projection.Category,
        projection.LanguageCode,
        projection.Status,
        projection.SeverityLevel,
        projection.CreatedAt,
        projection.UpdatedAt,
        projection.AgentId,
        projection.AgentName,
        projection.AgentAvatarUrl,
        projection.SlaDeadlineUtc,
        projection.SlaBreached,
        projection.SlaServiceCompleted,
        projection.Version);

    return Results.Ok(result);
});

app.MapPost("/projections/by-tokens", async (
    [FromBody] FilterByTokensRequest request,
    [FromServices] ITicketProjectionRepository repository,
    [FromServices] IPersonalInfoVaultClient personalInfoVaultClient,
    CancellationToken cancellationToken) =>
{
    if (request.Tokens is null || request.Tokens.Count == 0)
    {
        return Results.Ok(new List<TicketProjectionDto>());
    }

    var projections = await repository.GetByPersonTokensAsync(request.Tokens, cancellationToken);

    // Batch detokenization
    var tokens = projections.Select(p => p.PersonToken).Distinct().ToList();
    var personalInfos = await personalInfoVaultClient.GetBatchAsync(tokens, cancellationToken);
    var piiByToken = personalInfos.ToDictionary(p => p.PersonToken);

    var result = projections.Select(p =>
    {
        var pii = piiByToken.GetValueOrDefault(p.PersonToken);
        return new TicketProjectionDto(
            p.Id,
            p.InquiryId,
            pii?.Name ?? "Unknown",
            pii?.Email ?? "Unknown",
            p.Title,
            p.Description,
            p.Category,
            p.LanguageCode,
            p.Status,
            p.SeverityLevel,
            p.CreatedAt,
            p.UpdatedAt,
            p.AgentId,
            p.AgentName,
            p.AgentAvatarUrl,
            p.SlaDeadlineUtc,
            p.SlaBreached,
            p.SlaServiceCompleted,
            p.Version);
    }).ToList();

    return Results.Ok(result);
});

app.ExposeApiForFrontend();

app.Run();

public record FilterByTokensRequest(List<string> Tokens);

public record TicketProjectionDto(
    Guid Id,
    Guid InquiryId,
    string Name,
    string Email,
    string Title,
    string Description,
    string Category,
    string LanguageCode,
    string Status,
    string? SeverityLevel,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    Guid? AgentId,
    string AgentName,
    string AgentAvatarUrl,
    DateTimeOffset? SlaDeadlineUtc,
    bool? SlaBreached,
    bool SlaServiceCompleted,
    int Version);
