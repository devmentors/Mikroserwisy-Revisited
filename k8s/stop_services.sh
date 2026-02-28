#!/bin/bash
cd "$(dirname "$0")"

# Delete deployments
kubectl delete -f inquiries.yaml --ignore-not-found
kubectl delete -f tickets.yaml --ignore-not-found
kubectl delete -f translations.yaml --ignore-not-found
kubectl delete -f infrastructure.yaml --ignore-not-found
kubectl delete -f secrets.yaml --ignore-not-found

echo "All resources deleted."

# Optional: stop minikube
# minikube stop
