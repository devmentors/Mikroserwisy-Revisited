using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using TicketFlow.Services.Communication.Core.ExternalServices.Email.SendGrid;

namespace TicketFlow.Services.Communication.Core.ExternalServices.Email;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEmailServiceWithCircuitBreaker(
        this IServiceCollection services,
        string sendGridBaseUrl)
    {
        services.AddHttpClient<IEmailService, SendGridEmailService>(client =>
            {
                client.BaseAddress = new Uri(sendGridBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddStandardResilienceHandler(options =>
            {
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.MinimumThroughput = 5;

                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromMilliseconds(500);
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;

                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
            });

        return services;
    }
}
