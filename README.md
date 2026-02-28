<img width="1620" height="260" alt="mikro_revisited_lg_v2" src="https://github.com/user-attachments/assets/e81f0e0d-af13-4676-a9c1-01e40044f511" />

# Mikroserwisy: Revisited

W 2020 wydalismy kurs **Mikroserwisy .NET**, ktory pomogl tysiacom programistow poznac te architekture.

Od tamtej pory pracowalismy z wieloma systemami rozproszonymi - rozwijalismy je, naprawialismy, wyciagalismy z dlugu technicznego. Widzielismy co dziala, a co prowadzi donikad.

Dzis nie uczymy juz tylko "jak zaczac z mikroserwisami". Pokazujemy jak nie utonac, gdy system rosnie, zespol sie powieksza, a zlozonosc wymyka sie spod kontroli. Jak rozwijac architekture, ktora juz istnieje - bez rewolucji i przepisywania od zera.

Z samym kursem mozesz zapoznac sie na: https://mikroserwisy-revisited.pl

 # Czym jest projekt TicketFlow?

Ticketflow to **rozproszony** **system ticketowy**, w którym w prosty sposób zespół anglojezyczny jest w stanie obsługiwać zgłoszenia klientów z całego świata. Dodatkowo, by utrzymać SLA wynikające z umów podpisanych z klientami, system (poza wspieraniem procesu obsługi ticketow) wspiera agentów service desk w postaci przypomnień, alertowania czy prezentowania metryk na żywo.

Dla osób zaznajomionych z naszą twórczością, z punktu "domenowego" jest to rozwiniecie rozwazan nad aplikacją znaną z innego kursu: [**Messaging:Pragmatycznie**](https://messaging-pragmatycznie.pl/)


## Stos technologiczny

| Warstwa | Technologie |
|---------|------------|
| **Backend** | .NET 10, ASP.NET Core, Entity Framework Core |
| **Frontend** | React 18, shadcn/ui, CopilotKit |
| **Messaging** | RabbitMQ |
| **Baza danych** | PostgreSQL |
| **Obserwowalnosc** | OpenTelemetry, Jaeger, Prometheus, Grafana, Langfuse |
| **Infrastruktura** | Docker, Kubernetes |
| **AI** | Ollama, Microsoft.Extensions.AI, MCP, A2A |


## Jak uruchomic?

Wymagania:
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) lub [Rancher Desktop](https://rancherdesktop.io/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/)

### Szybki start

Uruchamiamy cala infrastrukture i wszystkie serwisy jednym poleceniem:
```
./run_ticketflow.sh
```

### Krok po kroku

1. Uruchamiamy infrastrukture (RabbitMQ, PostgreSQL, Jaeger, Prometheus, Grafana):
```
./run_infra.sh
```

2. Uruchamiamy wybrane serwisy backendowe, np.:
```
dotnet run --project src/Services/Tickets/TicketFlow.Services.Tickets.Api
```

3. Uruchamiamy frontend:
```
cd src_frontend/tickets && npm install && npm run dev
```

### Kubernetes

Alternatywnie, system mozna uruchomic na Kubernetesie:
```bash
./k8s/build-images.sh
./k8s/start_services.sh
```

## Moduly kursu

Repozytorium obejmuje material z 9 modulow tematycznych:

1. **Wprowadzenie** - setup projektu TicketFlow
2. **Kiedy mikroserwisy?** - kryteria wyboru architektury
3. **Komunikacja wewnetrzna** - sync, async, orkiestracja vs. choreografia, PACELC
4. **Dane w systemach rozproszonych** - zarzadzanie danymi miedzy serwisami
5. **Infrastruktura** - projektowanie infrastruktury i izolacja zasobow
6. **API zewnetrzne** - komunikacja zewnetrzna i wersjonowanie
7. **Governance** - metryki, dokumentacja, katalog serwisow
8. **AI** - integracja z AI (MCP, agenci)


## Wsparcie

