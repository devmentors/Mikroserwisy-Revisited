#!/bin/bash

# Kill services for L05 (3rd Party / Service Adapter) demo
# After running this script, start these services from your IDE:
# - Communication (port 5600)
# - MockSendGrid (port 6150)

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}Killing L05 (3rd Party) services...${NC}"

pkill -f "TicketFlow.Services.Communication" 2>/dev/null
pkill -f "MockSendGrid" 2>/dev/null

sleep 1

pkill -9 -f "TicketFlow.Services.Communication" 2>/dev/null
pkill -9 -f "MockSendGrid" 2>/dev/null

echo -e "${GREEN}Done. Now start from IDE:${NC}"
echo "  - Communication    (port 5600)"
echo "  - MockSendGrid     (port 6150)"
