#!/bin/bash
# k8s-memory-hog.sh - Demo: Memory Hog (Noisy Neighbor Problem)
#
# Pokazuje: Jeden pod bez limitów zżera RAM → inne pody dostają OOMKill
#
# Wymagania:
# - Rancher Desktop lub Docker Desktop (uruchomiony)
# - minikube (https://minikube.sigs.k8s.io/docs/start/)
# - kubectl
#
# Przed uruchomieniem zbuduj obrazy: ./k8s/build-images.sh

set -e

# Kolory
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_DIR="$(dirname "$SCRIPT_DIR")/k8s"

print_header() {
    echo ""
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}"
    echo ""
}

print_step() {
    echo -e "${GREEN}>>> $1${NC}"
}

wait_for_enter() {
    echo ""
    echo -e "${YELLOW}Naciśnij ENTER aby kontynuować...${NC}"
    read
}

cleanup() {
    print_step "Czyszczę środowisko..."
    kubectl delete all --all --wait=true 2>/dev/null || true
    minikube delete 2>/dev/null || true

    if kubectl config get-contexts 2>/dev/null | grep -q "rancher-desktop"; then
        kubectl config use-context rancher-desktop 2>/dev/null || true
    fi
    echo "Cleanup zakończony."
}

trap cleanup EXIT

# ============================================================
# SETUP
# ============================================================

print_header "DEMO: Memory Hog - Noisy Neighbor Problem"

# Sprawdź czy Docker działa
if ! docker info > /dev/null 2>&1; then
    echo "Docker nie działa! Uruchom Rancher Desktop lub Docker Desktop."
    exit 1
fi

# Sprawdź czy obrazy są zbudowane
if ! docker images | grep -q "ticketflow/tickets"; then
    echo "Obrazy Docker nie są zbudowane. Uruchom: ./k8s/build-images.sh"
    exit 1
fi

print_step "minikube start --driver=docker --memory=2048 --cpus=2"
minikube delete 2>/dev/null || true
minikube start --driver=docker --memory=2048 --cpus=2

print_step "minikube image load ticketflow/tickets:latest"
minikube image load ticketflow/tickets:latest
print_step "minikube image load ticketflow/inquiries:latest"
minikube image load ticketflow/inquiries:latest

wait_for_enter

# ============================================================
# Krok 1: Uruchom serwisy BEZ limitów
# ============================================================

print_header "Krok 1: Serwisy BEZ resource limits"

print_step "kubectl apply -f tickets.yaml"
kubectl apply -f "$K8S_DIR/secrets.yaml"
kubectl apply -f "$K8S_DIR/tickets.yaml"
print_step "kubectl apply -f inquiries.yaml"
kubectl apply -f "$K8S_DIR/inquiries.yaml"

print_step "kubectl wait --for=condition=ready pods --all --timeout=60s"
kubectl wait --for=condition=ready pods --all --timeout=60s 2>/dev/null || true

echo ""
print_step "kubectl get pods"
kubectl get pods
wait_for_enter

# ============================================================
# Krok 2: Dodaj memory-hog
# ============================================================

print_header "Krok 2: Memory Hog"

print_step "kubectl apply -f memory-hog.yaml"
kubectl apply -f "$K8S_DIR/demo/memory-hog.yaml"

print_step "kubectl wait --for=condition=ready pod/memory-hog --timeout=60s"
kubectl wait --for=condition=ready pod/memory-hog --timeout=60s 2>/dev/null || true

echo ""
print_step "kubectl get pods"
kubectl get pods
wait_for_enter

# ============================================================
# Krok 3: Monitoruj
# ============================================================

print_header "Krok 3: Monitorowanie (max 60s)"

SECONDS_WAITED=0
MAX_WAIT=60

while [ $SECONDS_WAITED -lt $MAX_WAIT ]; do
    # Sprawdź czy tickets/inquiries mają problemy
    VICTIM_STATUS=$(kubectl get pods --no-headers 2>/dev/null | grep -v "memory-hog" || echo "")

    if echo "$VICTIM_STATUS" | grep -qE "OOMKilled|Evicted|Error|CrashLoopBackOff"; then
        echo ""
        print_step "kubectl get pods"
        kubectl get pods
        echo ""
        print_step "Demo zakończone."
        wait_for_enter
        exit 0
    fi

    RESTARTS=$(echo "$VICTIM_STATUS" | awk '{sum += $4} END {print sum+0}')
    if [ "$RESTARTS" -gt 0 ]; then
        echo ""
        print_step "kubectl get pods"
        kubectl get pods
        echo ""
        print_step "Demo zakończone."
        wait_for_enter
        exit 0
    fi

    if [ $((SECONDS_WAITED % 5)) -eq 0 ]; then
        echo "[$SECONDS_WAITED s] kubectl get pods --no-headers | grep -v memory-hog"
        echo "$VICTIM_STATUS"
        echo ""
    fi

    sleep 1
    SECONDS_WAITED=$((SECONDS_WAITED + 1))
done

echo ""
print_step "kubectl get pods"
kubectl get pods 2>/dev/null || true
echo ""
echo "Timeout - nie udało się wywołać OOMKill w $MAX_WAIT sekund."
echo ""
print_step "Demo zakończone."
wait_for_enter
