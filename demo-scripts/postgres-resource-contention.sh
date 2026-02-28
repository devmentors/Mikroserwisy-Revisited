#!/bin/bash
# Demo: Resource Contention na wspoldzielonej instancji PostgreSQL
# Pokazuje jak obciazenie jednej bazy wplywa na inna baze

POSTGRES_CONTAINER="ticketflow-postgres"
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
RED='\033[0;31m'
NC='\033[0m'

LOAD_PIDS=()

cleanup() {
  echo ""
  echo -e "${YELLOW}Zatrzymuje obciazenie...${NC}"
  for pid in "${LOAD_PIDS[@]}"; do
    kill $pid 2>/dev/null
    wait $pid 2>/dev/null
  done
  echo -e "${YELLOW}Przywracam PostgreSQL (docker-compose recreate)...${NC}"
  cd "$(dirname "$0")/../compose" && docker-compose up -d --force-recreate postgres > /dev/null 2>&1
  sleep 3
  echo -e "${GREEN}Gotowe.${NC}"
}
trap cleanup EXIT

measure() {
  local start=$(python3 -c 'import time; print(time.time())')
  docker exec "$POSTGRES_CONTAINER" psql -U postgres -d "TicketFlow.Inquiries" -c "SELECT COUNT(*) FROM \"Inquiries\"" > /dev/null 2>&1
  local end=$(python3 -c 'import time; print(time.time())')
  python3 -c "print(f'{($end - $start) * 1000:.0f}')"
}

generate_load() {
  docker exec "$POSTGRES_CONTAINER" psql -U postgres -d "TicketFlow.Tickets" -c "DO \$\$BEGIN FOR i IN 1..100 LOOP PERFORM md5(random()::text) FROM generate_series(1,2000) s1 CROSS JOIN generate_series(1,2000) s2 ORDER BY random(); END LOOP; END\$\$;" > /dev/null 2>&1
}

echo ""

if ! docker ps --format '{{.Names}}' | grep -q "^${POSTGRES_CONTAINER}$"; then
  echo -e "${RED}BLAD: Kontener ${POSTGRES_CONTAINER} nie dziala.${NC}"
  echo "Uruchom: sh ./run_infra.sh"
  exit 1
fi

echo -e "${YELLOW}Ograniczam zasoby PostgreSQL (1 CPU, 512MB RAM)...${NC}"
docker update --cpus=1 --memory=512m --memory-swap=512m "$POSTGRES_CONTAINER" > /dev/null 2>&1
echo -e "${GREEN}OK${NC}"
echo ""

echo -e "${CYAN}===========================================================${NC}"
echo -e "${CYAN}Opcjonalnie - otworz dodatkowy terminal z docker stats:${NC}"
echo -e "${CYAN}===========================================================${NC}"
echo ""
echo "  docker stats $POSTGRES_CONTAINER"
echo ""
echo -e "${CYAN}Opcjonalnie - otworz frontend Inquiries:${NC}"
echo ""
echo "  http://localhost:21000"
echo ""

read -p "ENTER gdy jestes gotowy..."
echo ""

echo -e "${CYAN}Scenariusz:${NC}"
echo "  Dwie bazy na JEDNEJ instancji PostgreSQL (izolacja logiczna)."
echo "  Ktos z zespolu Tickets odpala ciezki raport/eksport."
echo "  Mierzymy jak to wplywa na zupelnie inna baze - Inquiries."
echo ""
echo -e "${CYAN}POMIAR (Inquiries):${NC}  SELECT COUNT(*) FROM \"Inquiries\""
echo -e "${CYAN}OBCIAZENIE (Tickets):${NC} 6 raportow - kazdy sortuje 4M wierszy"
echo ""

read -p "ENTER aby rozpoczac baseline..."
echo ""

echo -e "${YELLOW}[1/2] Baseline - Inquiries bez obciazenia:${NC}"
for i in 1 2 3 4 5; do
  echo "  Request $i: $(measure) ms"
done

echo ""
read -p "ENTER aby uruchomic raport na Tickets..."
echo ""

echo -e "${YELLOW}[2/2] Ktos odpalil ciezki raport na Tickets...${NC}"
echo ""

for i in $(seq 1 6); do
  generate_load &
  LOAD_PIDS+=($!)
done

sleep 3
echo -e "${YELLOW}Mierze Inquiries pod obciazeniem (sprobuj tez odswiezyc frontend!):${NC}"
echo ""

for i in $(seq 1 15); do
  echo "  Request $i: $(measure) ms"
done
