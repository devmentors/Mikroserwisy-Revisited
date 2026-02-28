using Microsoft.Extensions.AI;
using TicketFlow.Agents.Shared.Models;

namespace TicketFlow.Agents.ChatBot.Services;

public interface IPromptService
{
    string GetInstructions(UserContext user, AITool[] tools);
}
