using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Services.Inquiries.Core.Commands.SubmitInquiry;
using TicketFlow.Services.Inquiries.Core.Data.Repositories;
using TicketFlow.Services.Inquiries.Core.LanguageDetection;
using TicketFlow.Services.Inquiries.Core.Messaging.Publishing;
using TicketFlow.Services.Inquiries.IntegrationTests.Mocks;
using TicketFlow.Shared.Testing;
using Xunit;

namespace TicketFlow.Services.Inquiries.IntegrationTests.Messaging;

public class TranslationsServiceIntegrationTests : IAsyncLifetime
{
    private readonly MessagingIntegrationTestProvider<Program> _messagingTestProvider;
   
    private const string QueueV1 = "request-translation-v1-queue";
    private const string QueueV2 = "request-translation-v2-queue";

    public TranslationsServiceIntegrationTests()
    {
        _messagingTestProvider = new MessagingIntegrationTestProvider<Program>(services =>
        {
            services.AddSingleton<ILanguageDetector, TestLanguageDetector>();
            services.AddSingleton<IInquiriesRepository, InquiriesTestRepository>();
        });
    }
    
   public async Task InitializeAsync()
    {
        await _messagingTestProvider.RabbitMqContainer.StartAsync();

        _messagingTestProvider.Initialize(channel =>
        {
            channel.ExchangeDeclare(exchange: "inquiry-submitted-exchange", type: "direct", durable: false, autoDelete: false, arguments: null);
            channel.QueueDeclare(queue: QueueV1, durable: false, exclusive: false, autoDelete: false, arguments: null);
            channel.QueueDeclare(queue: QueueV2, durable: false, exclusive: false, autoDelete: false, arguments: null);
        });
    }

    public async Task DisposeAsync()
    {
        await _messagingTestProvider.DisposeAsync();
    }

    [Fact]
    public async Task SubmitInquiry_Should_Publish_To_RabbitMq_Queues_When_Language_IsNot_English()
    {
        var client = _messagingTestProvider.Factory.CreateClient();
        var languageDetector = _messagingTestProvider.Factory.Services.GetRequiredService<ILanguageDetector>();
        ((TestLanguageDetector) languageDetector).ReturnedLanguage = "pl";

        var command = new SubmitInquiry(
            Name: "John Doe",
            Email: "john@example.com",
            Title: "Test Title",
            Description: "Test Description",
            Category: "general");

        var response = await client.PostAsJsonAsync("/inquiries/submit", command);
        response.EnsureSuccessStatusCode();

        var receivedMessageV1 = await _messagingTestProvider.ConsumeMessagesAsync<RequestTranslationV1>(QueueV1);
        var receivedMessageV2 = await _messagingTestProvider.ConsumeMessagesAsync<RequestTranslationV2>(QueueV2);

        receivedMessageV1.Should().Match<RequestTranslationV1>(msg => msg.Text == command.Description);
        receivedMessageV2.Should().Match<RequestTranslationV2>(msg =>
            msg.Text == command.Description &&
            msg.LanguageCode == "pl");
    }
    
    [Fact]
    public async Task SubmitInquiry_ShouldNot_Publish_To_RabbitMq_Queues_When_Language_Is_English()
    {
        var client = _messagingTestProvider.Factory.CreateClient();
        var languageDetector = _messagingTestProvider.Factory.Services.GetRequiredService<ILanguageDetector>();
        ((TestLanguageDetector) languageDetector).ReturnedLanguage = "en";

        var inquiry = new SubmitInquiry(
            Name: "John Doe",
            Email: "john@example.com",
            Title: "Test Title",
            Description: "Test Description",
            Category: "general");

        var response = await client.PostAsJsonAsync("/inquiries/submit", inquiry);
        response.EnsureSuccessStatusCode();

        var receivedMessageV1 = await _messagingTestProvider.ConsumeMessagesAsync<RequestTranslationV1>(QueueV1);
        var receivedMessageV2 = await _messagingTestProvider.ConsumeMessagesAsync<RequestTranslationV2>(QueueV2);

        receivedMessageV1.Should().BeNull();
        receivedMessageV2.Should().BeNull();
    }
}