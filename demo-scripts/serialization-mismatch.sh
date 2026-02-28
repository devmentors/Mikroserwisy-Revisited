#!/bin/bash

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
NC='\033[0m'

echo ""
echo -e "${CYAN}=== Serialization Mismatch Demo ===${NC}"
echo ""

echo -e "${YELLOW}[1/2]${NC} Zatrzymuję Tickets..."
pkill -f "TicketFlow.Services.Tickets" 2>/dev/null
sleep 2

if pgrep -f "TicketFlow.Services.Tickets" > /dev/null; then
    echo -e "${RED}✗${NC} Nie udało się zatrzymać Tickets. Spróbuj ręcznie: pkill -9 -f Tickets"
    exit 1
fi
echo -e "${GREEN}✓${NC} Tickets zatrzymany"

echo ""
echo -e "${YELLOW}[2/2]${NC} Uruchamiam Tickets z PascalCase serializacją..."
echo ""
echo -e "${CYAN}────────────────────────────────────────────────────────────${NC}"
echo -e "  Inquiries wysyła:    ${GREEN}{ \"id\": \"...\", \"title\": \"...\" }${NC}"
echo -e "  Tickets oczekuje:    ${RED}{ \"Id\": \"...\", \"Title\": \"...\" }${NC}"
echo -e "${CYAN}────────────────────────────────────────────────────────────${NC}"
echo ""
echo -e "${YELLOW}TIP:${NC} Wyślij zgłoszenie przez UI i sprawdź logi Tickets"
echo -e "     Ticket nie powstanie - pola będą null/puste"
echo ""

cd "$(dirname "$0")/../src" || exit 1
dotnet run --project Services/Tickets/TicketFlow.Services.Tickets.Api -- --Serialization:UsePascalCase=true
