using TicketFlow.Services.BillingIntegration.Api;
using TicketFlow.Services.BillingIntegration.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCore(builder.Configuration);

var app = builder.Build();

app.MapBillingEndpoints();

app.MapGet("/", () => Results.Ok(new
{
    service = "Billing Integration Service",
    status = "Running"
}))
.WithName("Root");

app.Run();
