#!/bin/bash

set -e
PG_CONTAINER="ticketflow-postgres"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
NC='\033[0m'

# ── Step 1: Clean databases ──────────────────────────────────────────

echo ""
echo -e "${CYAN}Step 1: Cleaning databases...${NC}"

docker exec $PG_CONTAINER psql -U postgres -d "TicketFlow.Tickets" -c '
  TRUNCATE tickets."Tickets" CASCADE;
  TRUNCATE tickets."TicketScheduledActions" CASCADE;
  TRUNCATE outbox."OutboxMessages" CASCADE;
  TRUNCATE deduplication."DeduplicationEntries" CASCADE;
' > /dev/null 2>&1
echo -e "  ${GREEN}✓${NC} TicketFlow.Tickets cleaned"

docker exec $PG_CONTAINER psql -U postgres -d "TicketFlow.Inquiries" -c '
  TRUNCATE public."Inquiries" CASCADE;
' > /dev/null 2>&1
echo -e "  ${GREEN}✓${NC} TicketFlow.Inquiries cleaned"

docker exec $PG_CONTAINER psql -U postgres -d "TicketFlow.SLA" -c '
  TRUNCATE sla."DeadlineReminders" CASCADE;
  TRUNCATE outbox."OutboxMessages" CASCADE;
  TRUNCATE deduplication."DeduplicationEntries" CASCADE;
' > /dev/null 2>&1
echo -e "  ${GREEN}✓${NC} TicketFlow.SLA cleaned"

docker exec $PG_CONTAINER psql -U postgres -d "TicketFlow.Communication" -c '
  TRUNCATE communication."Messages" CASCADE;
  TRUNCATE communication."Alerts" CASCADE;
  TRUNCATE outbox."OutboxMessages" CASCADE;
  TRUNCATE deduplication."DeduplicationEntries" CASCADE;
' > /dev/null 2>&1
echo -e "  ${GREEN}✓${NC} TicketFlow.Communication cleaned"