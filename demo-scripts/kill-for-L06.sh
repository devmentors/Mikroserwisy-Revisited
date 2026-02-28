#!/bin/bash
# L06 - Observability
# Kill: EscalationAgent (Langfuse tracing, REACT instrumentation)
# Keep: Everything else running via run_ticketflow.sh

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

echo "L06: Killing EscalationAgent..."
pkill -f "TicketFlow.Agents.Escalation" 2>/dev/null && echo -e "${RED}Killed${NC} EscalationAgent (:8104)" || echo "EscalationAgent not running"

echo ""
echo -e "${GREEN}Run from IDE:${NC}"
echo "  src/Agents/TicketFlow.Agents.Escalation  →  :8104"
echo ""
echo -e "${GREEN}Observability UIs:${NC}"
echo "  Langfuse:    http://localhost:3102"
echo "  Jaeger:      http://localhost:16686"
echo "  Grafana:     http://localhost:3001"
