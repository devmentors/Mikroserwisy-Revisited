#!/bin/bash

# Run all A2A agents for TicketFlow
# Usage: ./run_all_agents.sh

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# PID tracking
SERVICE_PIDS=()

# Cleanup function
cleanup() {
    echo ""
    echo -e "${YELLOW}Stopping all agents...${NC}"
    for pid in "${SERVICE_PIDS[@]}"; do
        kill "$pid" 2>/dev/null
    done
    pkill -f "TicketFlow.Agents" 2>/dev/null
    sleep 1
    pkill -9 -f "TicketFlow.Agents" 2>/dev/null
    echo -e "${GREEN}All agents stopped${NC}"
    exit 0
}

# Set up trap for cleanup
trap cleanup SIGINT SIGTERM

echo "Starting TicketFlow A2A Agents..."

# Start agents in background
echo -e "${YELLOW}Starting ChatBot Agent (port 8000)...${NC}"
cd TicketFlow.Agents.ChatBot && dotnet run &
SERVICE_PIDS+=($!)
CHATBOT_PID=$!

echo -e "${YELLOW}Starting Escalation Agent (port 8104)...${NC}"
cd ../TicketFlow.Agents.Escalation && dotnet run &
SERVICE_PIDS+=($!)
ESCALATION_PID=$!

echo -e "${YELLOW}Starting KnowledgeBase Agent (port 8106)...${NC}"
cd ../TicketFlow.Agents.KnowledgeBase && dotnet run &
SERVICE_PIDS+=($!)
KB_PID=$!

echo ""
echo -e "${GREEN}All agents started!${NC}"
echo ""
echo "Agent PIDs:"
echo "  ChatBot:       $CHATBOT_PID"
echo "  Escalation:    $ESCALATION_PID"
echo "  KnowledgeBase: $KB_PID"
echo ""
echo "Ports:"
echo "  8000  - ChatBot Agent (AG-UI conversational agent)"
echo "  8104  - Escalation Agent (A2A autonomous agent)"
echo "  8106  - KnowledgeBase Agent (A2A knowledge retrieval)"
echo ""
echo "Note: MCP tools are exposed via Tickets.McpServer (port 5401) and Inquiries.McpServer (port 5501)"
echo ""
echo "Press Ctrl+C to stop all agents..."

# Wait for any process to exit
wait
