using LegacyBillingSystem;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapLegacyBillingSoapEndpoints();

app.MapGet("/", () => Results.Ok(new
{
    service = "Legacy Billing System",
    status = "Running"
}))
.WithName("Root");

app.Run();
