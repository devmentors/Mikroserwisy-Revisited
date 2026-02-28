#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

cd ./compose
docker build -t rabbitmq-che .
docker compose up -d

echo ""
echo -e "${CYAN}=======================================================${NC}"
echo -e "${CYAN}  Infrastruktura Docker uruchomiona!                   ${NC}"
echo -e "${CYAN}=======================================================${NC}"
echo ""
echo -e "${YELLOW}OBSERVABILITY:${NC}"
echo "-------------------------------------------------------"
printf "%-20s %-35s %s\n" "SERVICE" "URL" "CREDENTIALS"
echo "-------------------------------------------------------"
printf "%-20s %-35s %s\n" "Jaeger (traces)" "http://localhost:16686" "-"
printf "%-20s %-35s %s\n" "Grafana (metrics)" "http://localhost:3001" "admin / admin"
printf "%-20s %-35s %s\n" "Langfuse (LLM)" "http://localhost:3102" "student@ticketflow.local / student123"
printf "%-20s %-35s %s\n" "Prometheus" "http://localhost:9090" "-"
echo "-------------------------------------------------------"
echo ""
echo -e "${YELLOW}MESSAGING:${NC}"
echo "-------------------------------------------------------"
printf "%-20s %-35s %s\n" "RabbitMQ" "http://localhost:15672" "guest / guest"
echo "-------------------------------------------------------"
echo ""

# Check for local Ollama installation
echo -e "${YELLOW}Sprawdzam Ollama...${NC}"

if ! command -v ollama &> /dev/null; then
    echo ""
    echo -e "${RED}=======================================================${NC}"
    echo -e "${RED}  Ollama nie jest zainstalowana!                       ${NC}"
    echo -e "${RED}=======================================================${NC}"
    echo ""
    echo -e "Dla funkcji AI (ChatBot) zainstaluj Ollama:"
    echo ""
    echo -e "  ${CYAN}macOS:${NC}   brew install ollama"
    echo -e "  ${CYAN}Linux:${NC}   curl -fsSL https://ollama.com/install.sh | sh"
    echo -e "  ${CYAN}Windows:${NC} https://ollama.com/download"
    echo ""
    echo -e "Po instalacji uruchom ponownie ten skrypt."
    echo ""
    exit 0
fi

# Check if Ollama is running
if ! curl -s http://localhost:11434/api/tags > /dev/null 2>&1; then
    echo -e "${YELLOW}Uruchamiam Ollama...${NC}"
    ollama serve > /dev/null 2>&1 &
    sleep 3
fi

if ! curl -s http://localhost:11434/api/tags > /dev/null 2>&1; then
    echo -e "${RED}Nie mozna polaczyc z Ollama. Uruchom recznie: ollama serve${NC}"
    exit 1
fi

echo -e "${GREEN}Ollama dziala!${NC}"
echo ""

# Check and pull models if needed
pull_model_if_missing() {
    local MODEL=$1
    if ollama list 2>/dev/null | grep -q "^${MODEL}"; then
        echo -e "${GREEN}Model ${MODEL} - OK${NC}"
    else
        echo -e "${YELLOW}Pobieranie modelu ${MODEL}...${NC}"
        echo -e "${CYAN}(To moze potrwac kilka minut przy pierwszym uruchomieniu)${NC}"
        ollama pull "$MODEL"
        echo -e "${GREEN}Model ${MODEL} pobrany!${NC}"
    fi
}

pull_model_if_missing "llama3.1"

echo ""
echo -e "${GREEN}=======================================================${NC}"
echo -e "${GREEN}  Wszystko gotowe!                                     ${NC}"
echo -e "${GREEN}=======================================================${NC}"
echo ""
ollama list
echo ""
