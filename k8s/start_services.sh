#!/bin/bash
cd "$(dirname "$0")"

# Start minikube
minikube start

# Load images
minikube image load ticketflow/inquiries:latest
minikube image load ticketflow/tickets:latest
minikube image load ticketflow/translations:latest
minikube image load ticketflow/personalinfovault:latest
minikube image load ticketflow/apigateway:latest

# Apply secrets
kubectl apply -f secrets.yaml

# Start infrastructure first and wait until ready
echo "Starting infrastructure..."
kubectl apply -f infrastructure.yaml
kubectl rollout status deployment/rabbitmq --timeout=120s
kubectl rollout status deployment/postgres --timeout=120s

# Wait for RabbitMQ to be actually ready (accepting connections)
echo "Waiting for RabbitMQ to accept connections..."
kubectl wait --for=condition=ready pod -l app=rabbitmq --timeout=60s
sleep 5  # Extra buffer for RabbitMQ to fully initialize

# Wait for Postgres to be ready
echo "Waiting for Postgres to accept connections..."
kubectl wait --for=condition=ready pod -l app=postgres --timeout=60s

# Create databases
echo "Creating databases..."
POSTGRES_POD=$(kubectl get pod -l app=postgres -o jsonpath='{.items[0].metadata.name}')
kubectl exec $POSTGRES_POD -- psql -U postgres -c "CREATE DATABASE \"TicketFlow.PersonalInfoVault\";" 2>/dev/null || true
kubectl exec $POSTGRES_POD -- psql -U postgres -c "CREATE DATABASE \"TicketFlow.Inquiries\";" 2>/dev/null || true
kubectl exec $POSTGRES_POD -- psql -U postgres -c "CREATE DATABASE \"TicketFlow.Tickets\";" 2>/dev/null || true
kubectl exec $POSTGRES_POD -- psql -U postgres -c "CREATE DATABASE \"TicketFlow.Translations\";" 2>/dev/null || true

# Create tables (backup if EnsureCreated fails)
echo "Creating tables..."
kubectl exec $POSTGRES_POD -- psql -U postgres -d "TicketFlow.PersonalInfoVault" -c "CREATE TABLE IF NOT EXISTS \"PersonalInfos\" (\"Id\" uuid PRIMARY KEY, \"PersonToken\" text, \"Name\" text, \"Email\" text, \"IsAnonymized\" boolean, \"CreatedAt\" timestamptz, \"AnonymizedAt\" timestamptz);" 2>/dev/null || true

# Apply manifests
echo "Applying application manifests..."
kubectl apply -f personalinfovault.yaml
kubectl apply -f tickets.yaml
kubectl apply -f inquiries.yaml
kubectl apply -f translations.yaml

# Scale down to 1 replica to avoid migration race conditions
echo "Scaling down to 1 replica for migrations..."
kubectl scale deployment/personalinfovault --replicas=1
kubectl scale deployment/tickets --replicas=1
kubectl scale deployment/inquiries --replicas=1
kubectl scale deployment/translations --replicas=1

# Wait for single instances to be ready (migrations complete)
echo "Waiting for migrations to complete..."
kubectl rollout status deployment/personalinfovault --timeout=120s
kubectl rollout status deployment/tickets --timeout=120s
kubectl rollout status deployment/inquiries --timeout=120s
kubectl rollout status deployment/translations --timeout=120s

# Verify apps are actually running (not crashed)
echo "Verifying applications started correctly..."
sleep 3
for app in tickets inquiries translations personalinfovault; do
  if ! kubectl logs -l app=$app --tail=100 2>/dev/null | grep -q "Application started"; then
    echo "WARNING: $app may not have started correctly, checking logs..."
    kubectl logs -l app=$app --tail=20
  fi
done

# Scale up to desired replicas
echo "Scaling up to full replicas..."
kubectl scale deployment/tickets --replicas=3
kubectl scale deployment/inquiries --replicas=2
kubectl scale deployment/translations --replicas=2

# Wait for all replicas
kubectl rollout status deployment/tickets --timeout=60s
kubectl rollout status deployment/inquiries --timeout=60s
kubectl rollout status deployment/translations --timeout=60s

# Deploy API Gateway last (after all services are ready)
echo "Deploying API Gateway..."
kubectl apply -f apigateway.yaml
kubectl rollout status deployment/apigateway --timeout=60s

echo ""
echo "Done! Run port-forward for API Gateway:"
echo "  kubectl port-forward svc/apigateway-service 5100:80"
echo ""
echo "Optional (for debugging):"
echo "  kubectl port-forward svc/inquiries-service 5500:80"
echo "  kubectl port-forward svc/tickets-service 5400:80"
echo "  kubectl port-forward svc/translations-service 5800:80"
echo "  kubectl port-forward svc/personalinfovault-service 6100:80"
