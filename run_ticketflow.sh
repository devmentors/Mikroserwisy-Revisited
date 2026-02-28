#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Cleanup function
cleanup() {
    echo ""
    echo -e "${YELLOW}Stopping all services...${NC}"
    pkill -f "TicketFlow" 2>/dev/null
    pkill -f "next-server" 2>/dev/null
    pkill -f "next dev" 2>/dev/null
    pkill -f "node.*mock" 2>/dev/null
    sleep 1
    pkill -9 -f "TicketFlow" 2>/dev/null
    echo -e "${GREEN}All services stopped${NC}"
    exit 0
}

# Set up trap for cleanup
trap cleanup SIGINT SIGTERM

# Infra
echo -e "${BLUE}Starting infrastructure...${NC}"
sh ./run_infra.sh
echo "Waiting for infrastructure to start (10s)..."
sleep 10

# Run migrations
echo -e "${BLUE}Running database migrations...${NC}"
(cd src && ./run_all_migrations.sh) || { echo "Migrations failed"; exit 1; }

# Start all services in parallel
echo -e "${BLUE}Starting all services...${NC}"

# Start frontend applications
(cd src_frontend && ./run_all_fe.sh) &

# Start backend services
(cd src && ./run_all_be.sh) &

# Wait for services to start
sleep 8

# Print comprehensive service table
clear
echo ""
echo -e "${GREEN}=======================================================${NC}"
echo -e "${GREEN}              TICKETFLOW - ALL SERVICES                ${NC}"
echo -e "${GREEN}=======================================================${NC}"
echo ""
echo -e "${YELLOW}BACKEND SERVICES:${NC}"
echo "-------------------------------------------------------"
printf "%-25s %s\n" "SERVICE" "URL"
echo "-------------------------------------------------------"
printf "%-25s %s\n" "ClientsAPI (Edge)" "http://localhost:5050"
printf "%-25s %s\n" "API Gateway" "http://localhost:5100"
printf "%-25s %s\n" "BFF" "http://localhost:5200"
printf "%-25s %s\n" "Aggregation" "http://localhost:5300"
printf "%-25s %s\n" "Tickets" "http://localhost:5400"
printf "%-25s %s\n" "Inquiries" "http://localhost:5500"
printf "%-25s %s\n" "Communication" "http://localhost:5600"
printf "%-25s %s\n" "SLA" "http://localhost:5700"
printf "%-25s %s\n" "Translations" "http://localhost:5800"
printf "%-25s %s\n" "SystemMetrics" "http://localhost:5900"
printf "%-25s %s\n" "PersonalInfoVault" "http://localhost:6100"
printf "%-25s %s\n" "Anonymization" "http://localhost:6200"
echo ""
echo -e "${YELLOW}MCP SERVERS:${NC}"
echo "-------------------------------------------------------"
printf "%-25s %s\n" "Inquiries MCP" "http://localhost:5501"
printf "%-25s %s\n" "Tickets MCP" "http://localhost:5401"
echo ""
echo -e "${YELLOW}AI AGENTS:${NC}"
echo "-------------------------------------------------------"
printf "%-25s %s\n" "ChatBot Agent (AG-UI)" "http://localhost:8000"
printf "%-25s %s\n" "Escalation Agent (A2A)" "http://localhost:8104"
printf "%-25s %s\n" "KnowledgeBase Agent" "http://localhost:8106"
echo ""
echo -e "${YELLOW}FRONTEND APPLICATIONS:${NC}"
echo "-------------------------------------------------------"
printf "%-25s %s\n" "Inquiries" "http://localhost:21000"
printf "%-25s %s\n" "Tickets" "http://localhost:21001"
printf "%-25s %s\n" "Technical" "http://localhost:21002"
printf "%-25s %s\n" "Dashboard" "http://localhost:21003"
printf "%-25s %s\n" "Chatbot" "http://localhost:21200"
echo ""
echo -e "${YELLOW}INFRASTRUCTURE (Docker):${NC}"
echo "-------------------------------------------------------"
printf "%-25s %-35s %s\n" "SERVICE" "URL" "CREDENTIALS"
echo "-------------------------------------------------------"
printf "%-25s %-35s %s\n" "RabbitMQ" "http://localhost:15672" "guest / guest"
printf "%-25s %-35s %s\n" "PostgreSQL" "localhost:5432" "postgres / postgres"
echo ""
echo -e "${YELLOW}OBSERVABILITY:${NC}"
echo "-------------------------------------------------------"
printf "%-25s %-35s %s\n" "Jaeger (traces)" "http://localhost:16686" "-"
printf "%-25s %-35s %s\n" "Grafana (metrics)" "http://localhost:3001" "admin / admin"
printf "%-25s %-35s %s\n" "Langfuse (LLM)" "http://localhost:3102" "student@ticketflow.local / student123"
printf "%-25s %-35s %s\n" "Prometheus" "http://localhost:9090" "-"
echo ""
echo -e "${YELLOW}INFRASTRUCTURE (Local):${NC}"
echo "-------------------------------------------------------"
printf "%-25s %s\n" "Ollama (AI)" "http://localhost:11434"
echo "-------------------------------------------------------"
echo ""
echo "Press Ctrl+C to stop all services"
echo ""

# Wait for all background processes
wait
