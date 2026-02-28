#!/bin/bash
# 4.2b-resource-requests.sh - Demo: Resource Requests chronią przed OOMKill
#
# Pokazuje: ResourceQuota blokuje deployment gdy brak zasobów
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

print_warning() {
    echo -e "${YELLOW}!!! $1${NC}"
}

print_error() {
    echo -e "${RED}!!! $1${NC}"
}

wait_for_enter() {
    echo ""
    echo -e "${YELLOW}Naciśnij ENTER aby kontynuować...${NC}"
    read
}

cleanup() {
    print_step "Czyszczę środowisko..."
    kubectl delete all --all --wait=true 2>/dev/null || true
    kubectl delete resourcequota demo-quota 2>/dev/null || true
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

print_header "DEMO: Cannot Schedule Deployment"

# Sprawdź czy Docker działa
if ! docker info > /dev/null 2>&1; then
    print_error "Docker nie działa! Uruchom Rancher Desktop lub Docker Desktop."
    exit 1
fi
print_step "Docker działa ✓"

# Sprawdź czy obrazy są zbudowane
if ! docker images | grep -q "ticketflow/tickets"; then
    print_warning "Obrazy Docker nie są zbudowane!"
    echo "Uruchom najpierw: ./k8s/build-images.sh"
    exit 1
fi
print_step "Obrazy Docker są gotowe ✓"

print_step "Uruchamiam minikube..."
minikube delete 2>/dev/null || true
minikube start --driver=docker --memory=2048 --cpus=2

print_step "Ładuję obrazy do minikube..."
minikube image load ticketflow/tickets:latest
minikube image load ticketflow/inquiries:latest

wait_for_enter

# ============================================================
# DEMO
# ============================================================

print_header "Krok 1: ResourceQuota"

print_step "kubectl apply -f resource-quota.yaml"
kubectl apply -f "$K8S_DIR/demo/resource-quota.yaml"
echo ""
print_step "kubectl get resourcequota"
kubectl get resourcequota demo-quota
wait_for_enter

print_header "Krok 2: Pierwszy serwis (tickets)"

print_step "kubectl apply -f tickets-with-requests.yaml"
kubectl apply -f "$K8S_DIR/secrets.yaml"
kubectl apply -f "$K8S_DIR/demo/tickets-with-requests.yaml"

print_step "Czekam aż tickets wystartuje..."
kubectl wait --for=condition=ready pod -l app=tickets --timeout=60s 2>/dev/null || true

echo ""
print_step "kubectl get pods"
kubectl get pods
echo ""
print_step "kubectl get resourcequota"
kubectl get resourcequota demo-quota
wait_for_enter

print_header "Krok 3: Drugi serwis (inquiries)"

print_step "kubectl apply -f inquiries-with-requests.yaml"
kubectl apply -f "$K8S_DIR/demo/inquiries-with-requests.yaml"

sleep 3
echo ""
print_step "kubectl get pods"
kubectl get pods
echo ""
print_step "kubectl get rs"
kubectl get rs
echo ""
print_step "kubectl describe rs -l app=inquiries (Events)"
kubectl describe rs -l app=inquiries 2>/dev/null | grep -A 10 "Events:" || true
echo ""
print_step "kubectl get resourcequota"
kubectl get resourcequota demo-quota

echo ""
print_step "Demo zakończone."
wait_for_enter
