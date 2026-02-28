using TicketFlow.BFF.Dashboard;
using TicketFlow.BFF.Http.Aggregation;
using TicketFlow.BFF.Http.PersonalInfoVault;
using TicketFlow.BFF.Http.Sla;
using TicketFlow.BFF.Http.Tickets;
using TicketFlow.Shared.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMetrics(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddHttpClient<ITicketsClient, TicketsClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Services:Tickets")!);
});

builder.Services.AddHttpClient<ISlaClient, SlaClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Services:Sla")!);
});

builder.Services.AddHttpClient<IAggregationClient, AggregationClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Services:Aggregation")!);
});

builder.Services.AddHttpClient<IPersonalInfoVaultClient, PersonalInfoVaultClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Services:PersonalInfoVault")!);
});

builder.Services.AddScoped<ManagerDashboardService>();

var app = builder.Build();

app.UseMetrics();
app.UseCors("AllowAll");

app.MapGet("/", () => "BFF - Backend for Frontend (Scatter & Gather + Event-Driven Projections)");

app.MapGet("/manager/dashboard", async (
    ManagerDashboardService dashboardService,
    CancellationToken cancellationToken) =>
{
    var dashboard = await dashboardService.GetDashboard(cancellationToken);
    return Results.Ok(dashboard);
});

app.MapGet("/aggregation/projections", async (
    IAggregationClient aggregationClient,
    CancellationToken cancellationToken) =>
{
    var projections = await aggregationClient.GetProjectionsAsync(cancellationToken);
    return Results.Ok(projections);
});

app.MapGet("/aggregation/statistics", async (
    IAggregationClient aggregationClient,
    CancellationToken cancellationToken) =>
{
    var statistics = await aggregationClient.GetStatisticsAsync(cancellationToken);
    return Results.Ok(statistics);
});

app.MapGet("/aggregation/search", async (
    string query,
    ManagerDashboardService dashboardService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
    {
        return Results.BadRequest("Query must be at least 2 characters");
    }

    var projections = await dashboardService.SearchProjectionsAsync(query, cancellationToken);
    return Results.Ok(projections);
});

app.MapGet("/manager/dashboard/search", async (
    string query,
    ManagerDashboardService dashboardService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
    {
        return Results.BadRequest("Query must be at least 2 characters");
    }

    var tickets = await dashboardService.SearchTicketsAsync(query, cancellationToken);
    return Results.Ok(tickets);
});

app.Run();
