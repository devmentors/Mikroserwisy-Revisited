#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m' # No Color

echo -e "${RED}Stopping all backend services...${NC}"

# Kill all dotnet and TicketFlow processes
pkill -9 -f "dotnet run" 2>/dev/null
pkill -9 -f "TicketFlow" 2>/dev/null

sleep 2

# Check and kill processes on specific ports
for port in 5050 5100 5200 5300 5400 5500 5600 5700 5800 5900 6100 6200 8000; do
    pid=$(lsof -t -i:$port 2>/dev/null)
    if [ -n "$pid" ]; then
        echo "Killing process on port $port (PID: $pid)"
        kill -9 $pid 2>/dev/null
    fi
done

sleep 1

echo ""
echo -e "${GREEN}=============================================${NC}"
echo -e "${GREEN}       ALL SERVICES STOPPED                  ${NC}"
echo -e "${GREEN}=============================================${NC}"
echo ""

# Verify all ports are free
echo "Verifying ports are free:"
for port in 5050 5100 5200 5300 5400 5500 5600 5700 5800 5900 6100 6200 8000; do
    if lsof -i:$port >/dev/null 2>&1; then
        echo -e "${RED}✗ Port $port - STILL IN USE${NC}"
    else
        echo -e "${GREEN}✓ Port $port - FREE${NC}"
    fi
done
