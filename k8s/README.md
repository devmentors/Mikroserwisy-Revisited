# Kubernetes Setup

## Struktura

| Plik | Opis |
|------|------|
| `infrastructure.yaml` | RabbitMQ + PostgreSQL |
| `secrets.yaml` | Hasła do bazy danych |
| `inquiries.yaml` | Service + Deployment (2 repliki) |
| `tickets.yaml` | Service + Deployment (3 repliki) |
| `translations.yaml` | Service + Deployment (2 repliki) |
| `docker/` | Dockerfiles dla serwisów |

## Jak działa Service Discovery

Kubernetes Service = **DNS + Load Balancer**

- `http://tickets-service` → trafia do jednego z 3 podów Tickets
- `http://translations-service` → trafia do jednego z 2 podów Translations

Typed HTTP Clients w kodzie używają tych nazw DNS zamiast `localhost`.

## Wymagania

- Docker Desktop
- minikube (`brew install minikube`)
- kubectl

## Uruchomienie

```bash
# 1. Zbuduj obrazy (z głównego katalogu repo)
./k8s/build-images.sh

# 2. Uruchom serwisy w K8s
./k8s/start_services.sh

# 3. Port-forward (w osobnych terminalach)
kubectl port-forward svc/inquiries-service 5011:80
kubectl port-forward svc/tickets-service 5112:80
kubectl port-forward svc/translations-service 5274:80
```

## Testowanie Load Balancingu

```bash
./k8s/test_instance.sh
```

Wynik - różne instancje obsługują requesty:
```json
{"service":"Tickets","instance":"tickets-abc123"}
{"service":"Tickets","instance":"tickets-def456"}
{"service":"Tickets","instance":"tickets-ghi789"}
```

## Zatrzymanie

```bash
./k8s/stop_services.sh
minikube stop
```
