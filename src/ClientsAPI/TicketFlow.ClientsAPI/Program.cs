using System.Text.Json;
using Asp.Versioning;
using Scalar.AspNetCore;
using TicketFlow.ClientsAPI;
using TicketFlow.ClientsAPI.Api;
using TicketFlow.ClientsAPI.Http;
using TicketFlow.Shared.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:21000")  // Inquiries frontend
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddMetrics(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new QueryStringApiVersionReader("api-version"));
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info.Title = "TicketFlow Clients API";
        doc.Info.Version = "v1";
        doc.Info.Description = ApiDescriptions.V1.DocumentDescription;
        return Task.CompletedTask;
    });
    options.AddOperationTransformer((operation, _, _) =>
    {
        operation.Deprecated = true;
        return Task.CompletedTask;
    });
});

builder.Services.AddOpenApi("v2", options => 
    options.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info.Title = "TicketFlow Clients API";
        doc.Info.Version = "v2";
        doc.Info.Description = ApiDescriptions.V2.DocumentDescription;
        return Task.CompletedTask;
    }));

builder.Services.AddHttpClient<IInquiriesClient, InquiriesClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Inquiries"]!);
});

var app = builder.Build();

app.UseCors();
app.UseMetrics();
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "TicketFlow Clients API";
    options.DarkMode = true;
    options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.MapApi();

app.Run();
