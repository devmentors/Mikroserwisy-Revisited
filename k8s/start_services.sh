#!/bin/bash
cd "$(dirname "$0")"

# Start minikube
minikube start

# Load images
minikube image load ticketflow/inquiries:latest
minikube image load ticketflow/tickets:latest
minikube image load ticketflow/translations:latest

# Apply manifests
kubectl apply -f secrets.yaml
kubectl apply -f infrastructure.yaml
kubectl apply -f tickets.yaml
kubectl apply -f inquiries.yaml
kubectl apply -f translations.yaml

# Wait for pods
kubectl rollout status deployment/rabbitmq --timeout=60s
kubectl rollout status deployment/postgres --timeout=60s
kubectl rollout status deployment/tickets --timeout=60s
kubectl rollout status deployment/inquiries --timeout=60s
kubectl rollout status deployment/translations --timeout=60s

echo ""
echo "Done! Run port-forwards in separate terminals:"
echo "  kubectl port-forward svc/inquiries-service 5011:80"
echo "  kubectl port-forward svc/tickets-service 5112:80"
echo "  kubectl port-forward svc/translations-service 5274:80"
