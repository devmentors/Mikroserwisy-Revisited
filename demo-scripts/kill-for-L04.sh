#!/bin/bash
# L04 - Agenci (demo REACT w konsoli)
# Kill: Escalation + ChatBot (odpal z IDE żeby widzieć logi REACT)
# Keep: KnowledgeBase + MCP servers + all other services via run_ticketflow.sh

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

echo "L04: Killing agents for IDE restart..."
pkill -f "TicketFlow.Agents.Escalation" 2>/dev/null && echo -e "${RED}Killed${NC} EscalationAgent (:8104)" || echo "EscalationAgent not running"
pkill -f "TicketFlow.Agents.ChatBot" 2>/dev/null && echo -e "${RED}Killed${NC} ChatBot (:8000)" || echo "ChatBot not running"

echo ""
echo -e "${GREEN}Run from IDE:${NC}"
echo "  src/Agents/TicketFlow.Agents.Escalation      →  :8104"
echo "  src/ChatBot/TicketFlow.ChatBot                →  :8000"
echo ""
echo -e "${GREEN}Already running (from run_ticketflow.sh):${NC}"
echo "  KnowledgeBase (:8106), Tickets MCP (:5401), all services"
