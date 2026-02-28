using TicketFlow.Shared.Commands;

namespace TicketFlow.Services.Inquiries.Core.Commands.SubmitInquiry;

public sealed record SubmitInquiry(string Name, string Email, string Title, string Description, string Category) : ICommand;  