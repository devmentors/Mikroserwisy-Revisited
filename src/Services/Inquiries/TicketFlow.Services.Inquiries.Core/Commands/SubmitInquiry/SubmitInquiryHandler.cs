using Microsoft.Extensions.Logging;
using TicketFlow.Services.Inquiries.Core.Data.Models;
using TicketFlow.Services.Inquiries.Core.Data.Repositories;
using TicketFlow.Services.Inquiries.Core.Http;
using TicketFlow.Services.Inquiries.Core.LanguageDetection;
using TicketFlow.Services.Inquiries.Core.Messaging.Publishing;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Inquiries.Core.Commands.SubmitInquiry;

internal sealed class SubmitInquiryHandler(IInquiriesRepository repository, ILanguageDetector languageDetector,
    IMessagePublisher messagePublisher, IPersonalInfoVaultClient vaultClient, ILogger<SubmitInquiryHandler> logger) : ICommandHandler<SubmitInquiry>
{
    private const string EnglishLanguageCode = "en";
    public async Task HandleAsync(SubmitInquiry command, CancellationToken cancellationToken = default)
    {
        var (name, email, title, description, category) = command;
        var categoryParsed = ParseCategory(category);

        var personToken = await vaultClient.StorePersonalInfoAsync(name, email, cancellationToken);
        var inquiry = new Inquiry(personToken, title, description, categoryParsed);

        await repository.AddAsync(inquiry, cancellationToken);
        var detection = await languageDetector.DetectAsync(inquiry.Description, cancellationToken);
        var languageCode = detection.CodeOrUndetermined;

        if (CourseUtils.FeatureFlags.UseSharedContracts)
        {
            var inquiryReportedMessage = new Shared.Contracts.Inquiries.Events.InquirySubmitted(
                inquiry.Id,
                inquiry.PersonToken,
                inquiry.Title,
                inquiry.Description,
                inquiry.Category.ToString(),
                languageCode,
                inquiry.CreatedAt);
            await messagePublisher.PublishAsync(inquiryReportedMessage, cancellationToken: cancellationToken);
        }
        else
        {
            var inquiryReportedMessage = new InquirySubmitted(
                inquiry.Id,
                inquiry.PersonToken,
                inquiry.Title,
                inquiry.Description,
                inquiry.Category.ToString(),
                languageCode,
                inquiry.CreatedAt);
            await messagePublisher.PublishAsync(inquiryReportedMessage, cancellationToken: cancellationToken);
        }
        
        
        logger.LogInformation($"Inquiry with id: {inquiry.Id} submitted successfully.");
        
        if (languageCode is not EnglishLanguageCode)
        {
            var requestTranslationV1 = new RequestTranslationV1(inquiry.Description, inquiry.Id);
            var requestTranslationV2 = new RequestTranslationV2(inquiry.Description, languageCode, inquiry.Id);
            
            await messagePublisher.PublishAsync(requestTranslationV1, destination: "", routingKey: "request-translation-v1-queue", cancellationToken: cancellationToken);
            await messagePublisher.PublishAsync(requestTranslationV2, destination: "", routingKey: "request-translation-v2-queue", cancellationToken: cancellationToken);
            
            logger.LogInformation($"Translation for inquiry with id: {inquiry.Id} has been requested.");
        }
    }

    private static InquiryCategory ParseCategory(string category)
    {
        CapitalizeInput();
        var parseSucceeded = Enum.TryParse<InquiryCategory>(category, out var categoryParsed);
        
        if (!parseSucceeded)
        {
            categoryParsed = InquiryCategory.Other;
        }

        return categoryParsed;

        void CapitalizeInput()
        {
            category = category[0].ToString().ToUpper() + category.Substring(1, category.Length - 1);
        }
    }
}