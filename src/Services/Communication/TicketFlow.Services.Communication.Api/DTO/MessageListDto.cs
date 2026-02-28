using TicketFlow.Services.Communication.Core.Data.Models;

namespace TicketFlow.Services.Communication.Api.DTO;

public record MessageListDto(List<Message> Data, long Total);