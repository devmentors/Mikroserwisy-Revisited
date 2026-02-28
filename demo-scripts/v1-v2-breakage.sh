#!/bin/bash
# ============================================================
# L03 Demo: Kaskadowy breakage V1 → V2
# Wrapper data→items, wartość statusu New→new
# ============================================================

HOST="http://localhost:5050"
EMAIL="jan@example.com"

# Seed data
echo "Tworzenie testowego zgłoszenia..."
curl -s -o /dev/null -w "POST /v2/inquiries → %{http_code}\n" \
  -X POST "${HOST}/v2/inquiries" \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Test breakage demo\",\"description\":\"Demo V1 vs V2\",\"category\":\"Support\",\"name\":\"Jan Kowalski\",\"email\":\"${EMAIL}\"}"
echo ""

echo "============================================"
echo "  Kaskadowy breakage V1 → V2"
echo "============================================"
echo ""

# Krok 1: V1 — wszystko działa
echo "--- V1: .data[0].status ---"
RESULT=$(curl -s "${HOST}/v1/inquiries?email=${EMAIL}" | jq -r '.data[0].status')
echo "→ ${RESULT}"
echo ""

read -p "Enter..."
echo ""

# Krok 2: V2 z tą samą ścieżką
echo "--- V2: .data[0].status ---"
RESULT=$(curl -s "${HOST}/v2/inquiries?email=${EMAIL}" | jq -r '.data[0].status')
echo "→ ${RESULT}"
echo ""

read -p "Enter..."
echo ""

# Krok 3: Poprawka wrappera
echo "--- V2: .items[0].status ---"
RESULT=$(curl -s "${HOST}/v2/inquiries?email=${EMAIL}" | jq -r '.items[0].status')
echo "→ ${RESULT}"
echo ""

echo "============================================"
echo "  wrapper: data → items"
echo "  wartość: New → new"
echo "============================================"
