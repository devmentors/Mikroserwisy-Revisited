#!/bin/bash

# Kill services for L04 (Anti-Corruption Layer) demo
# After running this script, start these services from your IDE:
# - SLA (port 5700)
# - BillingIntegration (port 6000)
# - LegacyBillingSystem (port 6050)

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}Killing L04 (ACL) services...${NC}"

pkill -f "TicketFlow.Services.SLA" 2>/dev/null
pkill -f "TicketFlow.Services.BillingIntegration" 2>/dev/null
pkill -f "LegacyBillingSystem" 2>/dev/null

sleep 1

pkill -9 -f "TicketFlow.Services.SLA" 2>/dev/null
pkill -9 -f "TicketFlow.Services.BillingIntegration" 2>/dev/null
pkill -9 -f "LegacyBillingSystem" 2>/dev/null

echo -e "${GREEN}Done. Now start from IDE:${NC}"
echo "  - SLA                    (port 5700)"
echo "  - BillingIntegration     (port 6000)"
echo "  - LegacyBillingSystem    (port 6050)"
