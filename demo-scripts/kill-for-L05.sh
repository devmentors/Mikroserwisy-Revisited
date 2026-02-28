#!/bin/bash
# L05 - Developer experience
# Kill: Tickets MCP (MCP Inspector) + EscalationAgent (A2A Inspector)
# Keep: Everything else running via run_ticketflow.sh

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

echo "L05: Killing Tickets MCP + EscalationAgent..."
pkill -f "TicketFlow.Services.Tickets.McpServer" 2>/dev/null && echo -e "${RED}Killed${NC} Tickets MCP (:5401)" || echo "Tickets MCP not running"
pkill -f "TicketFlow.Agents.Escalation" 2>/dev/null && echo -e "${RED}Killed${NC} EscalationAgent (:8104)" || echo "EscalationAgent not running"

echo ""
echo -e "${GREEN}Run from IDE:${NC}"
echo "  src/Services/Tickets/TicketFlow.Services.Tickets.McpServer  →  :5401"
echo "  src/Agents/TicketFlow.Agents.Escalation                     →  :8104"
