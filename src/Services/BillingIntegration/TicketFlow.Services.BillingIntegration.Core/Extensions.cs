using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Services.BillingIntegration.Core.LegacyAdapter;
using TicketFlow.Services.BillingIntegration.Core.Repositories;

namespace TicketFlow.Services.BillingIntegration.Core;

public static class Extensions
{
    public static IServiceCollection AddCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var legacyBillingUrl = configuration["LegacyBilling:BaseUrl"]
            ?? "http://localhost:6050";

        services.AddHttpClient<LegacyBillingAdapter>(client =>
        {
            client.BaseAddress = new Uri(legacyBillingUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddScoped<IInvoiceRepository>(sp =>
            sp.GetRequiredService<LegacyBillingAdapter>());
        services.AddScoped<IPaymentStandingRepository>(sp =>
            sp.GetRequiredService<LegacyBillingAdapter>());

        return services;
    }
}
