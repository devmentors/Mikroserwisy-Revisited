using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Translations.Core.SynchronousIntegration;

public class GetTranslatedTextSynchronously : IQuery<string>
{
    public string Text { get; set; }
}