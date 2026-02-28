#!/bin/bash
# L01 - AI jako nowy konsument
# Kill: ChatBot (to run from IDE for demo)
# Keep: Everything else running via run_ticketflow.sh

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

echo "L01: Killing ChatBot..."
pkill -f "TicketFlow.Agents.ChatBot" 2>/dev/null && echo -e "${RED}Killed${NC} ChatBot (:8000)" || echo "ChatBot not running"

echo ""
echo -e "${GREEN}Run from IDE:${NC}"
echo "  src/Agents/TicketFlow.Agents.ChatBot  →  :8000"
