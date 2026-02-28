#!/bin/bash
set -e

cd "$(dirname "$0")/.."

echo "Building Docker images locally..."

echo "Building inquiries..."
docker build -f k8s/docker/Dockerfile.inquiries -t ticketflow/inquiries:latest .

echo "Building tickets..."
docker build -f k8s/docker/Dockerfile.tickets -t ticketflow/tickets:latest .

echo "Building translations..."
docker build -f k8s/docker/Dockerfile.translations -t ticketflow/translations:latest .

echo "Loading images into minikube..."
minikube image load ticketflow/inquiries:latest
minikube image load ticketflow/tickets:latest
minikube image load ticketflow/translations:latest

echo "Done! Images in minikube:"
minikube image ls | grep ticketflow
