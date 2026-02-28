using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Services.Translations.Core.Messaging.Consuming.RequestTranslation;
using TicketFlow.Services.Translations.Core.Messaging.Publishing;
using TicketFlow.Services.Translations.Core.Translations;
using TicketFlow.Services.Translations.IntegrationTests.Mocks;
using TicketFlow.Shared.Testing;
using Xunit;

namespace TicketFlow.Services.Translations.IntegrationTests.Messaging;

public class InquiriesServiceIntegrationTests : IAsyncLifetime
{
    private readonly MessagingIntegrationTestProvider<Program> _messagingTestProvider;
       
    private const string IncomingQueue = "request-translation-v2-queue";
    private const string TestQueue = "test-queue";
    private const string Exchange = "translation-completed-exchange";

    public InquiriesServiceIntegrationTests()
    {
        _messagingTestProvider = new MessagingIntegrationTestProvider<Program>(services =>
        {
            services.AddSingleton<ITranslationsService, TestTranslationsService>();
        });
    }
    
   public async Task InitializeAsync()
    {
        await _messagingTestProvider.RabbitMqContainer.StartAsync();

        _messagingTestProvider.Initialize(channel =>
        {
            channel.QueueDeclare(queue: IncomingQueue, durable: false, exclusive: false, autoDelete: false, arguments: null);
            channel.ExchangeDeclare(exchange: Exchange, type: "topic", durable: false, autoDelete: false, arguments: null);
            channel.QueueDeclare(queue: TestQueue, durable: false, exclusive: false, autoDelete: false, arguments: null);
            channel.QueueBind(TestQueue, Exchange, routingKey: "#", arguments: null);
        });
    }

    public async Task DisposeAsync()
    {
        await _messagingTestProvider.DisposeAsync();
    }

    [Fact]
    public async Task RequestTranslation_Should_Publish_To_RabbitMq_TranslationCompleted_When_Translation_Is_Successful()
    {
        var translationsService = _messagingTestProvider.Factory.Services.GetRequiredService<ITranslationsService>();
        var mockedResult = "TEST RESULT";
        ((TestTranslationsService) translationsService).TranslatedText = mockedResult;
        
        var message = new RequestTranslationV2("Test Text", "pl", Guid.NewGuid());
        
        var translationCompletedTask = _messagingTestProvider.ConsumeMessagesAsync<TranslationCompleted>(TestQueue, maxDelay: 10_000);
        _messagingTestProvider.Publish(message, "", routingKey: IncomingQueue);
        
        var translationCompleted = await translationCompletedTask;
        translationCompleted.Should().Match<TranslationCompleted>(msg => 
            msg.TranslatedText == mockedResult && msg.ReferenceId == message.ReferenceId);
    }
    
    [Fact]
    public async Task RequestTranslation_Should_Publish_To_RabbitMq_TranslationSkipped_When_Translation_Is_Missing()
    {
        var translationsService = _messagingTestProvider.Factory.Services.GetRequiredService<ITranslationsService>();
        var mockedResult = "";
        ((TestTranslationsService) translationsService).TranslatedText = mockedResult;
        
        var message = new RequestTranslationV2("Test Text", "pl", Guid.NewGuid());
        
        var translationCompletedTask = _messagingTestProvider.ConsumeMessagesAsync<TranslationSkipped>(TestQueue);
        _messagingTestProvider.Publish(message, "", routingKey: IncomingQueue);
        
        var translationCompleted = await translationCompletedTask;
        translationCompleted.Should().Match<TranslationSkipped>(msg => msg.ReferenceId == message.ReferenceId);
    }
}