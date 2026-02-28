#!/bin/bash
# ============================================================
# L03 Demo: Hyrum's Law — klient parsuje format błędu
# V1: framework exception → klient łapie "Required parameter"
# V2: Problem Details → klient nie rozpoznaje błędu
# ============================================================

HOST="http://localhost:5050"
PATTERN="Required parameter"

echo "============================================"
echo "  Symulacja klienta"
echo "  grep '${PATTERN}' w response body"
echo "============================================"
echo ""

echo "--- V1: GET /v1/inquiries ---"
BODY=$(curl -s "${HOST}/v1/inquiries")
echo "${BODY}"
echo ""
if echo "${BODY}" | grep -q "${PATTERN}"; then
    echo "Klient → ERROR: brak wymaganego parametru"
else
    echo "Klient → (brak błędu)"
fi
echo ""

read -p "Enter..."
echo ""

echo "--- V2: GET /v2/inquiries ---"
BODY=$(curl -s "${HOST}/v2/inquiries")
echo "${BODY}"
echo ""
if echo "${BODY}" | grep -q "${PATTERN}"; then
    echo "Klient → ERROR: brak wymaganego parametru"
else
    echo "Klient → (brak błędu)"
fi
echo ""
