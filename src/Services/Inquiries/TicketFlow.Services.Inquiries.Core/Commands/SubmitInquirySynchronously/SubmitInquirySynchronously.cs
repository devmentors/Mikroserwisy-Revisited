using TicketFlow.Shared.Commands;

namespace TicketFlow.Services.Inquiries.Core.Commands.SubmitInquirySynchronously;

public record SubmitInquirySynchronously(string Name, string Email, string Title, string Description, string Category) : ICommand;  