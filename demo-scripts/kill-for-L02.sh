#!/bin/bash
# L02 - MCP - Model Context Protocol
# Kill: ChatBot + Tickets API + Tickets MCP + Inquiries API + Inquiries MCP
# Keep: Everything else running via run_ticketflow.sh

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

echo "L02: Killing ChatBot + Tickets + Inquiries..."
pkill -f "TicketFlow.Agents.ChatBot" 2>/dev/null && echo -e "${RED}Killed${NC} ChatBot (:8000)" || echo "ChatBot not running"
pkill -f "TicketFlow.Services.Tickets.Api" 2>/dev/null && echo -e "${RED}Killed${NC} Tickets API (:5400)" || echo "Tickets API not running"
pkill -f "TicketFlow.Services.Tickets.McpServer" 2>/dev/null && echo -e "${RED}Killed${NC} Tickets MCP (:5401)" || echo "Tickets MCP not running"
pkill -f "TicketFlow.Services.Inquiries.Api" 2>/dev/null && echo -e "${RED}Killed${NC} Inquiries API (:5500)" || echo "Inquiries API not running"
pkill -f "TicketFlow.Services.Inquiries.McpServer" 2>/dev/null && echo -e "${RED}Killed${NC} Inquiries MCP (:5501)" || echo "Inquiries MCP not running"

echo ""
echo -e "${GREEN}Run from IDE:${NC}"
echo "  src/Services/Tickets/TicketFlow.Services.Tickets.Api        →  :5400"
echo "  src/Services/Tickets/TicketFlow.Services.Tickets.McpServer  →  :5401"
echo "  src/Services/Inquiries/TicketFlow.Services.Inquiries.Api    →  :5500"
echo "  src/Services/Inquiries/TicketFlow.Services.Inquiries.McpServer →  :5501"
echo "  src/Agents/TicketFlow.Agents.ChatBot                        →  :8000"
