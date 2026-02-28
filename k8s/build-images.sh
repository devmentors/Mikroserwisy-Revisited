#!/bin/bash
set -e

cd "$(dirname "$0")/.."

echo "Building Docker images locally..."

echo "Building inquiries..."
docker build --no-cache -f k8s/docker/Dockerfile.inquiries -t ticketflow/inquiries:latest .

echo "Building tickets..."
docker build --no-cache -f k8s/docker/Dockerfile.tickets -t ticketflow/tickets:latest .

echo "Building translations..."
docker build --no-cache -f k8s/docker/Dockerfile.translations -t ticketflow/translations:latest .

echo "Building personalinfovault..."
docker build --no-cache -f k8s/docker/Dockerfile.personalinfovault -t ticketflow/personalinfovault:latest .

echo "Building apigateway..."
docker build --no-cache -f k8s/docker/Dockerfile.apigateway -t ticketflow/apigateway:latest .

echo "Removing old images from minikube..."
minikube image rm docker.io/ticketflow/inquiries:latest 2>/dev/null || true
minikube image rm docker.io/ticketflow/tickets:latest 2>/dev/null || true
minikube image rm docker.io/ticketflow/translations:latest 2>/dev/null || true
minikube image rm docker.io/ticketflow/personalinfovault:latest 2>/dev/null || true
minikube image rm docker.io/ticketflow/apigateway:latest 2>/dev/null || true

echo "Loading images into minikube..."
minikube image load ticketflow/inquiries:latest
minikube image load ticketflow/tickets:latest
minikube image load ticketflow/translations:latest
minikube image load ticketflow/personalinfovault:latest
minikube image load ticketflow/apigateway:latest

echo "Done! Images in minikube:"
minikube image ls | grep ticketflow
