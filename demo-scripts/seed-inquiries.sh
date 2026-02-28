#!/bin/bash
# Seed demo data for L02: cleans DBs, creates inquiries, diversifies ticket statuses
# Usage: sh demo-scripts/seed-inquiries.sh [--claude-code]
# Requires: running infrastructure (postgres, rabbitmq) + all backend services

set -e

CONFIGURE_CLAUDE_CODE=false
if [ "$1" = "--claude-code" ]; then
  CONFIGURE_CLAUDE_CODE=true
fi

INQUIRIES_URL="http://localhost:5500"
TICKETS_URL="http://localhost:5400"
PG_CONTAINER="ticketflow-postgres"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
NC='\033[0m'

AGENT_ZIEMOWIT="00000000-0000-0000-0000-000000000002"
AGENT_KUNEGUNDA="00000000-0000-0000-0000-000000000003"

# User IDs for inquiry scoping (used by Inquiries MCP)
# Must match src_frontend/inquiries/lib/clients.ts
USER_ANNA="10000000-0000-0000-0000-000000000002"
USER_PIOTR="10000000-0000-0000-0000-000000000003"

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

# ── Step 2: Submit inquiries ─────────────────────────────────────────

echo ""
echo -e "${CYAN}Step 2: Submitting 20 inquiries...${NC}"

submit() {
  local name="$1" email="$2" category="$3" title="$4" description="$5" userId="$6"
  local userHeader=""
  [ -n "$userId" ] && userHeader="-H X-User-Id:${userId}"
  local status=$(curl -s -o /dev/null -w "%{http_code}" -X POST "${INQUIRIES_URL}/inquiries/submit" \
    -H "Content-Type: application/json" \
    $userHeader \
    -d "{\"name\":\"${name}\",\"email\":\"${email}\",\"category\":\"${category}\",\"title\":\"${title}\",\"description\":\"${description}\"}")
  if [ "$status" -ge 200 ] && [ "$status" -lt 300 ]; then
    echo -e "  ${GREEN}✓${NC} ${title}"
  else
    echo -e "  ${RED}✗${NC} ${title} (HTTP ${status})"
  fi
  sleep 0.05
}

# Anna's inquiries (13) - includes tickets that will be qualified, assigned, blocked, and resolved
submit "Anna Kowalska"     "anna.k@firma.pl"     "technical" "Nie mogę się zalogować do systemu"              "Od wczoraj nie mogę się zalogować. Próbowałem resetować hasło ale link nie przychodzi." "$USER_ANNA"
submit "Anna Kowalska"     "anna.k@firma.pl"     "billing"   "Błąd płatności przy zamówieniu"                 "Przy próbie płatności kartą dostaję komunikat Transaction failed. Próbowałem 3 razy." "$USER_ANNA"
submit "Anna Kowalska"     "anna.k@firma.pl"     "technical" "Aplikacja crashuje po aktualizacji"             "Po ostatniej aktualizacji do wersji 4.2 aplikacja zamyka się natychmiast po uruchomieniu." "$USER_ANNA"
submit "Anna Kowalska"     "anna.k@firma.pl"     "general"   "Prośba o reset hasła"                          "Potrzebuję pilnie zresetować hasło. Nie mam dostępu do starego maila." "$USER_ANNA"
submit "Anna Kowalska"     "anna.k@firma.pl"     "technical" "Brak dostępu do panelu administracyjnego"       "Moje konto ma rolę admin ale panel administracyjny zwraca 403 Forbidden." "$USER_ANNA"
submit "Anna Kowalska"     "anna.k@firma.pl"     "billing"   "Faktura nie zgadza się z zamówieniem"           "Faktura nr 2024/001 pokazuje kwotę 500zł a zamówienie było na 350zł." "$USER_ANNA"
submit "Anna Kowalska"     "anna.k@firma.pl"     "technical" "Strona ładuje się bardzo wolno"                 "Strona główna ładuje się ponad 30 sekund. Inne strony działają normalnie." "$USER_ANNA"
submit "Anna Kowalska"     "anna.k@firma.pl"     "general"   "Nie otrzymałem potwierdzenia emailem"           "Złożyłem zamówienie 2 dni temu i nadal nie dostałem potwierdzenia na email." "$USER_ANNA"
submit "Anna Kowalska"     "anna.k@firma.pl"     "billing"   "Dwukrotne pobranie opłaty z karty"             "Z mojej karty pobrano opłatę dwukrotnie za to samo zamówienie. Proszę o zwrot." "$USER_ANNA"
submit "Anna Kowalska"     "anna.k@firma.pl"     "technical" "Problem z eksportem danych do CSV"              "Eksport danych do CSV generuje pusty plik. Testowałem w Chrome i Firefox." "$USER_ANNA"
submit "Anna Kowalska"     "anna.k@firma.pl"     "technical" "Nie działa wyszukiwarka produktów"              "Wyszukiwarka zwraca 0 wyników niezależnie od frazy. Wcześniej działała." "$USER_ANNA"
submit "Anna Kowalska"     "anna.k@firma.pl"     "general"   "Konto zablokowane bez powodu"                  "Moje konto zostało zablokowane. Nie dostawałem żadnych ostrzeżeń." "$USER_ANNA"
submit "Anna Kowalska"     "anna.k@firma.pl"     "technical" "Błąd 500 przy składaniu reklamacji"             "Przy składaniu reklamacji dostaję błąd 500. Dzieje się to powtarzalnie." "$USER_ANNA"

# Piotr's inquiries (7) - all remain BeforeQualification
submit "Piotr Nowak"       "piotr.n@corp.com"    "technical" "Integracja z API zwraca timeout"                "Nasze API integration timeout-uje po 30s. Wcześniej odpowiedzi były w 2s." "$USER_PIOTR"
submit "Piotr Nowak"       "piotr.n@corp.com"    "general"   "Brak powiadomień push"                         "Od tygodnia nie dostaję żadnych powiadomień push mimo włączonych ustawień." "$USER_PIOTR"
submit "Piotr Nowak"       "piotr.n@corp.com"    "billing"   "Nieprawidłowe naliczanie rabatów"               "Rabat 20% nie jest naliczany mimo aktywnego kodu promocyjnego." "$USER_PIOTR"
submit "Piotr Nowak"       "piotr.n@corp.com"    "general"   "Nie mogę zmienić adresu dostawy"               "System nie pozwala zmienić adresu dostawy po złożeniu zamówienia." "$USER_PIOTR"
submit "Piotr Nowak"       "piotr.n@corp.com"    "technical" "Panel raportów nie wyświetla danych"            "Panel raportów pokazuje pustą stronę. W konsoli widzę błąd JavaScript." "$USER_PIOTR"
submit "Piotr Nowak"       "piotr.n@corp.com"    "technical" "Problemy z uwierzytelnianiem dwuskładnikowym"   "2FA przez SMS nie działa - kod nigdy nie przychodzi. Authenticator działa OK." "$USER_PIOTR"
submit "Piotr Nowak"       "piotr.n@corp.com"    "general"   "Zamówienie stuck w statusie Processing"         "Zamówienie 4521 jest w statusie Processing od 5 dni. Normalnie trwa to max 24h." "$USER_PIOTR"

# ── Step 3: Wait for messaging pipeline ──────────────────────────────

echo ""
echo -e "${CYAN}Step 3: Waiting for Inquiries → Tickets pipeline...${NC}"

for i in $(seq 1 30); do
  COUNT=$(curl -s "${TICKETS_URL}/tickets/?page=1&limit=1" | grep -o '"totalCount":[0-9]*' | grep -o '[0-9]*')
  if [ "$COUNT" -ge 20 ] 2>/dev/null; then
    echo -e "  ${GREEN}✓${NC} ${COUNT} tickets created"
    break
  fi
  printf "\r  ${YELLOW}⏳${NC} Waiting... (${COUNT:-0}/20 tickets so far, ${i}/30s)"
  sleep 1
done

if [ "${COUNT:-0}" -lt 20 ] 2>/dev/null; then
  echo ""
  echo -e "  ${RED}⚠${NC}  Only ${COUNT:-0} tickets created after 30s. Continuing with what we have."
fi

# ── Step 4: Diversify ticket statuses ────────────────────────────────

echo ""
echo -e "${CYAN}Step 4: Diversifying ticket statuses...${NC}"

# Fetch all ticket IDs
TICKET_IDS=$(curl -s "${TICKETS_URL}/tickets/?page=1&limit=50" | python3 -c "
import sys, json
data = json.load(sys.stdin)
for t in data['data']:
    print(t['id'])
" 2>/dev/null)

IDS=()
while IFS= read -r line; do
  [ -n "$line" ] && IDS+=("$line")
done <<< "$TICKET_IDS"

TOTAL=${#IDS[@]}
echo "  Found ${TOTAL} tickets to diversify"

if [ "$TOTAL" -lt 10 ]; then
  echo -e "  ${RED}✗${NC} Not enough tickets. Is the pipeline running?"
  exit 1
fi

qualify() {
  local id="$1" type="$2" severity="$3"
  curl -s -o /dev/null -X POST "${TICKETS_URL}/tickets/${id}/qualify" \
    -H "Content-Type: application/json" \
    -d "{\"ticketType\":\"${type}\",\"severityLevel\":\"${severity}\"}"
}

assign() {
  local id="$1" agent="$2"
  curl -s -o /dev/null -X POST "${TICKETS_URL}/tickets/${id}/assign/${agent}" \
    -H "Content-Type: application/json" -d "{}"
}

block() {
  local id="$1" reason="$2"
  local encoded=$(python3 -c "import urllib.parse, sys; print(urllib.parse.quote(sys.argv[1]))" "$reason")
  curl -s -o /dev/null -X POST "${TICKETS_URL}/tickets/${id}/block/${encoded}"
}

resolve() {
  local id="$1" resolution="$2"
  local encoded=$(python3 -c "import urllib.parse, sys; print(urllib.parse.quote(sys.argv[1]))" "$resolution")
  curl -s -o /dev/null -X POST "${TICKETS_URL}/tickets/${id}/resolve/${encoded}"
}

# Tickets 0-4: Qualify with varied severity, assign to agents
# → Status: Qualified + Assigned (in progress)
for i in 0 1 2 3 4; do
  TYPES=("Incident" "Incident" "Question" "Incident" "Question")
  SEVS=("Critical" "High" "Medium" "High" "Low")
  AGENTS=("$AGENT_ZIEMOWIT" "$AGENT_KUNEGUNDA" "$AGENT_ZIEMOWIT" "$AGENT_KUNEGUNDA" "$AGENT_ZIEMOWIT")
  qualify "${IDS[$i]}" "${TYPES[$i]}" "${SEVS[$i]}"
  assign "${IDS[$i]}" "${AGENTS[$i]}"
done
echo -e "  ${GREEN}✓${NC} 5 tickets: Qualified + Assigned (1 Critical, 2 High, 1 Medium, 1 Low)"

# Tickets 5-7: Qualify but DON'T assign
# → Status: Qualified, unassigned (needs attention!)
qualify "${IDS[5]}" "Incident" "Critical"
qualify "${IDS[6]}" "Incident" "High"
qualify "${IDS[7]}" "Question" "Medium"
echo -e "  ${GREEN}✓${NC} 3 tickets: Qualified + UNASSIGNED (1 Critical, 1 High, 1 Medium)"

# Tickets 8-9: Qualify, assign, then block
# → Status: Blocked
qualify "${IDS[8]}" "Incident" "High"
assign "${IDS[8]}" "$AGENT_ZIEMOWIT"
block "${IDS[8]}" "Waiting for external vendor response"

qualify "${IDS[9]}" "Incident" "Medium"
assign "${IDS[9]}" "$AGENT_KUNEGUNDA"
block "${IDS[9]}" "Customer not responding to follow-up"
echo -e "  ${GREEN}✓${NC} 2 tickets: Blocked"

# Tickets 10-12: Qualify, assign, resolve
# → Status: Resolved
qualify "${IDS[10]}" "Question" "Low"
assign "${IDS[10]}" "$AGENT_ZIEMOWIT"
resolve "${IDS[10]}" "Password reset link sent successfully"

qualify "${IDS[11]}" "Question" "Low"
assign "${IDS[11]}" "$AGENT_KUNEGUNDA"
resolve "${IDS[11]}" "Account unlocked after identity verification"

qualify "${IDS[12]}" "Incident" "Medium"
assign "${IDS[12]}" "$AGENT_ZIEMOWIT"
resolve "${IDS[12]}" "Bug fixed in version 4.2.1 hotfix"
echo -e "  ${GREEN}✓${NC} 3 tickets: Resolved"

# Tickets 13-19: Leave as BeforeQualification
# → Status: BeforeQualification (needs qualification)
echo -e "  ${GREEN}✓${NC} 7 tickets: BeforeQualification (untouched)"

# ── Summary ──────────────────────────────────────────────────────────

echo ""
echo -e "${GREEN}╔════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║           SEED DATA READY                  ║${NC}"
echo -e "${GREEN}╠════════════════════════════════════════════╣${NC}"
echo -e "${GREEN}║${NC}  BeforeQualification:  7 tickets           ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  Qualified+Assigned:   5 tickets           ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  Qualified+Unassigned: 3 tickets (!)       ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  Blocked:              2 tickets           ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  Resolved:             3 tickets           ${GREEN}║${NC}"
echo -e "${GREEN}╠════════════════════════════════════════════╣${NC}"
echo -e "${GREEN}║${NC}  TOTAL:               20 tickets           ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  Critical unassigned:  1 (!)               ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  High unassigned:      1 (!)               ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  Ziemowit load:       3/10 + 1 blocked     ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  Kunegunda load:      2/10 + 1 blocked     ${GREEN}║${NC}"
echo -e "${GREEN}╠════════════════════════════════════════════╣${NC}"
echo -e "${GREEN}║${NC}  Anna (13 inquiries): ${USER_ANNA}  ${GREEN}║${NC}"
echo -e "${GREEN}║${NC}  Piotr (7 inquiries): ${USER_PIOTR}  ${GREEN}║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════════╝${NC}"

# ── Step 5 (optional): Configure Claude Code MCP ──────────────────

if [ "$CONFIGURE_CLAUDE_CODE" = true ]; then
  echo ""
  echo -e "${CYAN}Step 5: Configuring Claude Code MCP servers...${NC}"
  CLAUDE_JSON="$HOME/.claude.json"
  if command -v python3 &> /dev/null; then
    python3 -c "
import json, os, sys

path = sys.argv[1]
user_id = sys.argv[2]

settings = {}
if os.path.exists(path):
    with open(path) as f:
        settings = json.load(f)

servers = settings.get('mcpServers', {})
servers['ticketflow-tickets'] = {
    'type': 'http',
    'url': 'http://localhost:5401/',
    'headers': {'X-User-Role': 'supervisor'}
}
servers['ticketflow-inquiries'] = {
    'type': 'http',
    'url': 'http://localhost:5501/',
    'headers': {'X-User-Role': 'client', 'X-User-Id': user_id}
}
settings['mcpServers'] = servers

with open(path, 'w') as f:
    json.dump(settings, f, indent=2)
" "$CLAUDE_JSON" "$USER_ANNA"
    echo -e "  ${GREEN}✓${NC} Added ticketflow-tickets MCP server"
    echo -e "  ${GREEN}✓${NC} Added ticketflow-inquiries MCP server (User: Anna - ${USER_ANNA})"
    echo -e "  ${GREEN}✓${NC} Written to ${CLAUDE_JSON} (restart Claude Code to apply)"
  else
    echo -e "  ${YELLOW}⚠${NC}  python3 not found, add MCP servers manually to ${CLAUDE_JSON}"
  fi
fi
echo ""
