#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# PID tracking
SERVICE_PIDS=()

# Cleanup function
cleanup() {
    echo ""
    echo -e "  ${YELLOW}Stopping all services...${NC}"
    for pid in "${SERVICE_PIDS[@]}"; do
        kill "$pid" 2>/dev/null
    done
    pkill -f "TicketFlow" 2>/dev/null
    sleep 1
    pkill -9 -f "TicketFlow" 2>/dev/null
    echo -e "  ${GREEN}✓${NC} All services stopped"
    exit 0
}

# Set up trap for cleanup
trap cleanup SIGINT SIGTERM

# Clear screen for clean output
clear

echo ""
echo -e "${CYAN}╔═══════════════════════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║           ${BLUE}TicketFlow Backend Services${CYAN}                      ║${NC}"
echo -e "${CYAN}╚═══════════════════════════════════════════════════════════╝${NC}"
echo ""

# Kill existing BE processes
echo -e "  ${YELLOW}◉${NC} Killing existing backend processes..."
pkill -f "TicketFlow" 2>/dev/null
sleep 1
pkill -9 -f "TicketFlow" 2>/dev/null
echo -e "  ${GREEN}✓${NC} Existing processes killed"

# Spinner function
spin() {
    local pid=$1
    local delay=0.1
    local spinstr='⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏'
    while ps -p $pid > /dev/null 2>&1; do
        for i in $(seq 0 9); do
            printf "\r  ${YELLOW}${spinstr:$i:1}${NC} $2"
            sleep $delay
        done
    done
    printf "\r"
}

# Pre-build all projects
printf "  ${YELLOW}◉${NC} Building all projects..."
dotnet build ../TicketFlow.sln --verbosity quiet > /dev/null 2>&1 &
BUILD_PID=$!
spin $BUILD_PID "Building all projects..."
wait $BUILD_PID
BUILD_STATUS=$?

if [ $BUILD_STATUS -ne 0 ]; then
    echo -e "  ${RED}✗${NC} Build failed! Run 'dotnet build' manually to see errors."
    exit 1
fi
echo -e "  ${GREEN}✓${NC} Build completed successfully"

# Start services silently
printf "  ${YELLOW}◉${NC} Starting services..."

# Start API Gateway
(cd ApiGateway/TicketFlow.ApiGateway && dotnet run --no-build > /dev/null 2>&1) &
SERVICE_PIDS+=($!)

# Start BFF
(cd BFF/TicketFlow.BFF && dotnet run --no-build > /dev/null 2>&1) &
SERVICE_PIDS+=($!)

# Start ClientsAPI (External Edge Layer)
(cd ClientsAPI/TicketFlow.ClientsAPI && dotnet run --no-build > /dev/null 2>&1) &
SERVICE_PIDS+=($!)

# Start all API services
for dir in Services/*/*.Api/; do
    if [ -d "$dir" ]; then
        (cd "$dir" && dotnet run --no-build > /dev/null 2>&1) &
        SERVICE_PIDS+=($!)
    fi
done

# Start all MCP Servers
for dir in Services/*/*.McpServer/; do
    if [ -d "$dir" ]; then
        (cd "$dir" && dotnet run --no-build > /dev/null 2>&1) &
        SERVICE_PIDS+=($!)
    fi
done

# Start all Agents (C#)
(cd Agents/TicketFlow.Agents.ChatBot && dotnet run --no-build > /dev/null 2>&1) &
SERVICE_PIDS+=($!)
(cd Agents/TicketFlow.Agents.Escalation && dotnet run --no-build > /dev/null 2>&1) &
SERVICE_PIDS+=($!)
(cd Agents/TicketFlow.Agents.KnowledgeBase && dotnet run --no-build > /dev/null 2>&1) &
SERVICE_PIDS+=($!)

# Wait for services to start
sleep 8
echo -e "\r  ${GREEN}✓${NC} All services started          "

# Clear and print final table
echo ""
echo -e "${GREEN}╔═══════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║                    SERVICES READY                         ║${NC}"
echo -e "${GREEN}╠═══════════════════════════════════════════════════════════╣${NC}"
echo -e "${GREEN}║${NC}  SERVICE                  URL                            ${GREEN}║${NC}"
echo -e "${GREEN}╠═══════════════════════════════════════════════════════════╣${NC}"
echo -e "${GREEN}║${NC}  API Gateway              ${CYAN}http://localhost:5100${NC}          ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  BFF                      ${CYAN}http://localhost:5200${NC}          ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  ClientsAPI (Edge)        ${CYAN}http://localhost:5050${NC}          ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  Aggregation              ${CYAN}http://localhost:5300${NC}          ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  Tickets                  ${CYAN}http://localhost:5400${NC}          ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  Inquiries                ${CYAN}http://localhost:5500${NC}          ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  Communication            ${CYAN}http://localhost:5600${NC}          ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  SLA                      ${CYAN}http://localhost:5700${NC}          ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  Translations             ${CYAN}http://localhost:5800${NC}          ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  SystemMetrics            ${CYAN}http://localhost:5900${NC}          ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  PersonalInfoVault        ${CYAN}http://localhost:6100${NC}          ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  Anonymization            ${CYAN}http://localhost:6200${NC}          ${GREEN}║${NC}"
echo -e "${GREEN}╠═══════════════════════════════════════════════════════════╣${NC}"
echo -e "${GREEN}║${NC}  ${YELLOW}MCP SERVERS${NC}                                               ${GREEN}║${NC}"
echo -e "${GREEN}╠═══════════════════════════════════════════════════════════╣${NC}"
echo -e "${GREEN}║${NC}  Inquiries MCP            ${CYAN}http://localhost:5501${NC}          ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  Tickets MCP              ${CYAN}http://localhost:5401${NC}          ${GREEN}║${NC}"
echo -e "${GREEN}╠═══════════════════════════════════════════════════════════╣${NC}"
echo -e "${GREEN}║${NC}  ${YELLOW}AI AGENTS${NC}                                                 ${GREEN}║${NC}"
echo -e "${GREEN}╠═══════════════════════════════════════════════════════════╣${NC}"
echo -e "${GREEN}║${NC}  ChatBot Agent (AG-UI)    ${CYAN}http://localhost:8000${NC}          ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  Escalation Agent         ${CYAN}http://localhost:8104${NC}          ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  KnowledgeBase Agent      ${CYAN}http://localhost:8106${NC}          ${GREEN}║${NC}"
echo -e "${GREEN}╚═══════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "  ${YELLOW}TIP:${NC} Press ${RED}Ctrl+C${NC} to stop all services"
echo ""

# Wait for all background processes
wait
