#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

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

# Start mock API
(cd src_frontend && ./run_mock_api.sh) &

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
printf "%-25s %s\n" "API Gateway" "http://localhost:5100"
printf "%-25s %s\n" "BFF" "http://localhost:5200"
printf "%-25s %s\n" "Aggregation" "http://localhost:5300"
printf "%-25s %s\n" "Tickets" "http://localhost:5400"
printf "%-25s %s\n" "Inquiries" "http://localhost:5500"
printf "%-25s %s\n" "Communication" "http://localhost:5600"
printf "%-25s %s\n" "SLA" "http://localhost:5700"
printf "%-25s %s\n" "Translations" "http://localhost:5800"
printf "%-25s %s\n" "SystemMetrics" "http://localhost:5900"
echo ""
echo -e "${YELLOW}FRONTEND APPLICATIONS:${NC}"
echo "-------------------------------------------------------"
printf "%-25s %s\n" "Inquiries" "http://localhost:21000"
printf "%-25s %s\n" "Tickets" "http://localhost:21001"
printf "%-25s %s\n" "Technical" "http://localhost:21002"
printf "%-25s %s\n" "Dashboard" "http://localhost:21003"
echo ""
echo -e "${YELLOW}INFRASTRUCTURE:${NC}"
echo "-------------------------------------------------------"
printf "%-25s %s\n" "RabbitMQ" "http://localhost:15672"
printf "%-25s %s\n" "PostgreSQL" "localhost:5432"
printf "%-25s %s\n" "Prometheus" "http://localhost:9090"
printf "%-25s %s\n" "Grafana" "http://localhost:3000"
echo "-------------------------------------------------------"
echo ""
echo "Press Ctrl+C to stop all services"
echo ""

# Wait for all background processes
wait
