using TicketFlow.Services.SystemMetrics.Core;
using TicketFlow.Shared.AnomalyGeneration.HttpApi;
using TicketFlow.Shared.AspNetCore;
using TicketFlow.Shared.Metrics;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddCore(builder.Configuration)
    .AddApiForFrontendConfigured();

var app = builder.Build();

app.UseMetrics();
app.ExposeApiForFrontend();
app.UseAnomalyEndpoints();
app.ExposeLiveMetrics();

app.Run();