#!/bin/bash

# TicketFlow Developer Portal - Startup Script

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PORTAL_DIR="$SCRIPT_DIR/ticketflow-developer-portal"

echo "=== TicketFlow Developer Portal ==="

# Kill processes on ports 3000 and 7007
echo "Stopping any existing processes on ports 3000 and 7007..."
lsof -ti:3000 | xargs kill -9 2>/dev/null
lsof -ti:7007 | xargs kill -9 2>/dev/null
sleep 2

# Check if portal directory exists
if [ ! -d "$PORTAL_DIR" ]; then
    echo "Error: Portal directory not found: $PORTAL_DIR"
    exit 1
fi

cd "$PORTAL_DIR"

# Check if node_modules exists
if [ ! -d "node_modules" ]; then
    echo "Installing dependencies..."
    yarn install
fi

# Start the portal
echo "Starting Developer Portal..."
echo "Frontend: http://localhost:3000"
echo "Backend:  http://localhost:7007"
echo ""
yarn start
