using Microsoft.Extensions.Logging;
using TicketFlow.Services.Inquiries.Core.Clients;
using TicketFlow.Services.Inquiries.Core.Data.Models;
using TicketFlow.Services.Inquiries.Core.Data.Repositories;
using TicketFlow.Services.Inquiries.Core.Http;
using TicketFlow.Services.Inquiries.Core.LanguageDetection;
using TicketFlow.Shared.Commands;

namespace TicketFlow.Services.Inquiries.Core.Commands.SubmitInquirySynchronously;

public class SubmitInquirySynchronouslyHandler(
    IInquiriesRepository repository,
    ILanguageDetector languageDetector,
    ITicketsClient ticketsClient,
    ITranslationsClient translationsClient,
    IPersonalInfoVaultClient vaultClient,
    ILogger<SubmitInquirySynchronouslyHandler> logger) : ICommandHandler<SubmitInquirySynchronously>
{
    private const string EnglishLanguageCode = "en";

    public async Task HandleAsync(SubmitInquirySynchronously command, CancellationToken cancellationToken = default)
    {
        var (name, email, title, description, category) = command;

        var personToken = await vaultClient.StorePersonalInfoAsync(name, email, cancellationToken);
        var categoryParsed = ParseCategory(category);
        var inquiry = new Inquiry(personToken, title, description, categoryParsed);

        await repository.AddAsync(inquiry, cancellationToken);

        var detection = await languageDetector.DetectAsync(inquiry.Description, cancellationToken);
        var languageCode = detection.CodeOrUndetermined;
        var translatedDescription = string.Empty;

        if (languageCode is not EnglishLanguageCode)
        {
            logger.LogInformation($"Translation for inquiry with id: {inquiry.Id} has been requested.");
            translatedDescription = await translationsClient.TranslateAsync(inquiry.Description, cancellationToken);
        }

        var createTicketCommand = new CreateTicketSynchronously(
            inquiry.Id,
            inquiry.PersonToken,
            inquiry.Title,
            inquiry.Description,
            translatedDescription,
            inquiry.Category.ToString(),
            languageCode,
            inquiry.CreatedAt);

        await ticketsClient.CreateTicketAsync(createTicketCommand, cancellationToken);
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