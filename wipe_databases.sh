#!/bin/bash

# Wipe Inquiries, Tickets, and Communication databases
# This script drops and recreates the databases, then runs migrations

echo "=== Wiping Inquiries, Tickets, and Communication databases ==="

# Database names (use dots as per EF Core naming convention)
DATABASES=("TicketFlow.Inquiries" "TicketFlow.Tickets" "TicketFlow.Communication")

# Drop and recreate each database
for DB in "${DATABASES[@]}"; do
    echo "Dropping database: $DB"
    docker exec ticketflow-postgres psql -U postgres -c "DROP DATABASE IF EXISTS \"$DB\";" 2>/dev/null

    echo "Creating database: $DB"
    docker exec ticketflow-postgres psql -U postgres -c "CREATE DATABASE \"$DB\";" 2>/dev/null
done

echo ""
echo "=== Running migrations ==="

# Run migrations for each service
echo "Running Inquiries migrations..."
cd ./src/Services/Inquiries/TicketFlow.Services.Inquiries.Api && dotnet ef database update --no-build 2>/dev/null || dotnet ef database update
cd ../../../..

echo "Running Tickets migrations..."
cd ./src/Services/Tickets/TicketFlow.Services.Tickets.Api && dotnet ef database update --no-build 2>/dev/null || dotnet ef database update
cd ../../../..

echo "Running Communication migrations..."
cd ./src/Services/Communication/TicketFlow.Services.Communication.Api && dotnet ef database update --no-build 2>/dev/null || dotnet ef database update
cd ../../../..

echo ""
echo "=== Done! Databases wiped and migrations applied ==="
