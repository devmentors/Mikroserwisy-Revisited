#!/bin/bash
# L03 - Zabezpieczenie MCP
# Kill: Tickets MCP + Inquiries MCP + Inquiries MCP AntiPattern + Inquiries API
# Keep: Everything else running via run_ticketflow.sh

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

echo "L03: Killing Tickets MCP + Inquiries MCP + AntiPattern..."
pkill -f "TicketFlow.Services.Tickets.McpServer" 2>/dev/null && echo -e "${RED}Killed${NC} Tickets MCP (:5401)" || echo "Tickets MCP not running"
pkill -f "TicketFlow.Services.Inquiries.Api" 2>/dev/null && echo -e "${RED}Killed${NC} Inquiries API (:5500)" || echo "Inquiries API not running"
pkill -f "TicketFlow.Services.Inquiries.McpServer" 2>/dev/null && echo -e "${RED}Killed${NC} Inquiries MCP (:5501) + AntiPattern (:5502)" || echo "Inquiries MCP not running"

echo ""
echo -e "${GREEN}Run from IDE:${NC}"
echo "  src/Services/Tickets/TicketFlow.Services.Tickets.McpServer              →  :5401"
echo "  src/Services/Inquiries/TicketFlow.Services.Inquiries.Api                →  :5500"
echo "  src/Services/Inquiries/TicketFlow.Services.Inquiries.McpServer          →  :5501"
echo "  src/Services/Inquiries/TicketFlow.Services.Inquiries.McpServer.AntiPattern →  :5502"
