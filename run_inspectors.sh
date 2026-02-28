#!/bin/bash

# =============================================================================
# TicketFlow - Inspector Tools Launcher
# =============================================================================
# This script launches developer inspection tools for MCP and A2A protocols.
# Run this INDEPENDENTLY from run_ticketflow.sh - it's for debugging/testing.
#
# Prerequisites:
#   - Node.js 18+ (for MCP Inspector)
#   - Docker (for A2A Inspector)
#   - MCP servers running (Tickets :5401, Inquiries :5501)
#   - A2A agents running (ChatBot :8000, Escalation :8104, KB :8106)
#
# Usage:
#   ./run_inspectors.sh           # Launch all inspectors
#   ./run_inspectors.sh mcp       # Launch only MCP Inspector
#   ./run_inspectors.sh a2a       # Launch only A2A Inspector
#   ./run_inspectors.sh stop      # Stop all inspectors
# =============================================================================

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSPECTORS_DIR="$SCRIPT_DIR/tools/inspectors"
A2A_INSPECTOR_DIR="$INSPECTORS_DIR/a2a-inspector"

# MCP Server URLs (C# SDK MapMcp() uses root "/" for Streamable HTTP)
TICKETS_MCP_URL="http://localhost:5401/"
INQUIRIES_MCP_URL="http://localhost:5501/"

# A2A Agent URLs
CHATBOT_URL="http://localhost:8000"
ESCALATION_URL="http://localhost:8104"
KB_URL="http://localhost:8106"

# Inspector Ports
MCP_INSPECTOR_UI_PORT=6274
MCP_INSPECTOR_PROXY_PORT=6277
A2A_INSPECTOR_PORT=6280

print_header() {
    echo ""
    echo -e "${CYAN}============================================================${NC}"
    echo -e "${CYAN}  $1${NC}"
    echo -e "${CYAN}============================================================${NC}"
    echo ""
}

print_status() {
    echo -e "${GREEN}[+]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[!]${NC} $1"
}

print_error() {
    echo -e "${RED}[x]${NC} $1"
}

print_info() {
    echo -e "${BLUE}[i]${NC} $1"
}

check_command() {
    if ! command -v "$1" &> /dev/null; then
        print_error "$1 is not installed"
        return 1
    fi
    return 0
}

check_service() {
    local url="$1"
    local name="$2"
    if curl -s --connect-timeout 2 "$url/health" > /dev/null 2>&1; then
        print_status "$name is running"
        return 0
    else
        print_warning "$name is not running at $url"
        return 1
    fi
}

# =============================================================================
# MCP Inspector Functions
# =============================================================================

launch_mcp_inspector() {
    print_header "MCP Inspector"

    if ! check_command "npx"; then
        print_error "npx (Node.js) is required for MCP Inspector"
        print_info "Install Node.js 18+: https://nodejs.org/"
        return 1
    fi

    print_info "MCP Inspector connects to MCP servers via HTTP transport"
    print_info ""

    # Check MCP server availability
    echo -e "${BLUE}Checking MCP servers...${NC}"
    check_service "http://localhost:5401" "Tickets MCP (:5401)" || true
    check_service "http://localhost:5501" "Inquiries MCP (:5501)" || true

    echo ""
    print_info "MCP Card endpoints (for manual inspection):"
    print_info "  - Tickets:   http://localhost:5401/mcp/card"
    print_info "  - Inquiries: http://localhost:5501/mcp/card"
    echo ""

    print_status "Launching MCP Inspector..."
    print_info "UI will be available at: http://localhost:$MCP_INSPECTOR_UI_PORT"
    print_info ""
    print_info "To connect to MCP servers in the Inspector UI:"
    print_info "  1. Select 'Streamable HTTP' transport"
    print_info "  2. Enter URL: http://localhost:5401/ (Tickets) or http://localhost:5501/ (Inquiries)"
    print_info "  3. Add header: X-User-Role: supervisor (or agent/admin)"
    print_info ""

    # Launch MCP Inspector in background
    npx @modelcontextprotocol/inspector \
        --port $MCP_INSPECTOR_UI_PORT \
        --proxy-port $MCP_INSPECTOR_PROXY_PORT &

    MCP_PID=$!
    echo $MCP_PID > "$INSPECTORS_DIR/mcp_inspector.pid" 2>/dev/null || true

    print_status "MCP Inspector launched (PID: $MCP_PID)"
    print_info "Press Ctrl+C to stop"
}

# =============================================================================
# A2A Inspector Functions
# =============================================================================

setup_a2a_inspector() {
    print_info "Setting up A2A Inspector..."

    mkdir -p "$INSPECTORS_DIR"

    if [ ! -d "$A2A_INSPECTOR_DIR" ]; then
        print_status "Cloning A2A Inspector repository..."
        git clone https://github.com/a2aproject/a2a-inspector.git "$A2A_INSPECTOR_DIR"
    else
        print_status "A2A Inspector already cloned"
    fi
}

launch_a2a_inspector() {
    print_header "A2A Inspector"

    print_info "A2A Inspector connects to A2A agents via JSON-RPC 2.0"
    print_info ""

    # Check A2A agent availability
    echo -e "${BLUE}Checking A2A agents...${NC}"
    check_service "$CHATBOT_URL" "ChatBot Agent (:8000)" || true
    check_service "$ESCALATION_URL" "Escalation Agent (:8104)" || true
    check_service "$KB_URL" "KB Agent (:8106)" || true

    echo ""
    print_info "A2A Card endpoints:"
    print_info "  - Escalation: http://localhost:8104/.well-known/agent-card.json"
    print_info "  - KB Agent:   http://localhost:8106/.well-known/agent-card.json"
    echo ""

    # Clone if needed
    setup_a2a_inspector

    cd "$A2A_INSPECTOR_DIR"

    # Check for python3.12+ (required by a2a-inspector)
    local PYTHON_CMD=""
    if command -v python3.12 &> /dev/null; then
        PYTHON_CMD="python3.12"
    elif command -v python3.13 &> /dev/null; then
        PYTHON_CMD="python3.13"
    elif command -v python3 &> /dev/null; then
        local py_version=$(python3 -c 'import sys; print(sys.version_info.minor)')
        if [ "$py_version" -ge 12 ]; then
            PYTHON_CMD="python3"
        fi
    fi

    if [ -z "$PYTHON_CMD" ]; then
        print_error "Python 3.12+ is required for A2A Inspector"
        print_info "Install with: brew install python@3.12"
        print_info "Or: pyenv install 3.12.0 && pyenv global 3.12.0"
        return 1
    fi

    print_status "Using $PYTHON_CMD ($($PYTHON_CMD --version))"

    # Create venv if needed (or recreate if wrong Python version)
    if [ -d ".venv" ]; then
        local venv_py_version=$(.venv/bin/python3 -c 'import sys; print(sys.version_info.minor)' 2>/dev/null || echo "0")
        if [ "$venv_py_version" -lt 12 ]; then
            print_warning "Existing venv has old Python, recreating..."
            rm -rf .venv
        fi
    fi

    if [ ! -d ".venv" ]; then
        print_status "Creating Python virtual environment with $PYTHON_CMD..."
        $PYTHON_CMD -m venv .venv
    fi

    # Build frontend if needed
    if [ ! -f "frontend/public/script.js" ]; then
        print_status "Building frontend..."
        cd frontend
        npm install --silent 2>/dev/null || npm install
        npm run build
        cd ..
    fi

    # Activate venv and install dependencies
    source .venv/bin/activate
    print_status "Upgrading pip..."
    pip install --upgrade pip -q 2>/dev/null || pip install --upgrade pip
    print_status "Installing Python dependencies (this may take a moment)..."
    pip install -q . 2>/dev/null || pip install .

    print_status "Launching A2A Inspector on port $A2A_INSPECTOR_PORT..."

    cd backend
    uvicorn app:app --host 127.0.0.1 --port $A2A_INSPECTOR_PORT &
    A2A_PID=$!
    echo $A2A_PID > "$INSPECTORS_DIR/a2a_inspector.pid"
    cd "$SCRIPT_DIR"

    sleep 2

    print_status "A2A Inspector launched (PID: $A2A_PID)"
    print_info "UI available at: http://localhost:$A2A_INSPECTOR_PORT"
    print_info ""
    print_info "To connect to A2A agents in the Inspector UI:"
    print_info "  1. Enter agent URL: http://localhost:8104 (Escalation)"
    print_info "  2. Click 'Fetch Card' to retrieve agent capabilities"
    print_info "  3. Use the chat interface to interact with the agent"
    print_info ""
    print_info "A2A endpoints:"
    print_info "  - Escalation: http://localhost:8104/a2a"
    print_info "  - KB Agent:   http://localhost:8106/a2a"
}


# =============================================================================
# Stop Functions
# =============================================================================

stop_inspectors() {
    print_header "Stopping Inspectors"

    # Stop MCP Inspector
    if [ -f "$INSPECTORS_DIR/mcp_inspector.pid" ]; then
        MCP_PID=$(cat "$INSPECTORS_DIR/mcp_inspector.pid")
        if kill -0 "$MCP_PID" 2>/dev/null; then
            kill "$MCP_PID" 2>/dev/null || true
            print_status "Stopped MCP Inspector (PID: $MCP_PID)"
        fi
        rm -f "$INSPECTORS_DIR/mcp_inspector.pid"
    fi

    # Stop any npx processes for MCP inspector
    pkill -f "@modelcontextprotocol/inspector" 2>/dev/null || true

    # Stop A2A Inspector
    if [ -f "$INSPECTORS_DIR/a2a_inspector.pid" ]; then
        A2A_PID=$(cat "$INSPECTORS_DIR/a2a_inspector.pid")
        if kill -0 "$A2A_PID" 2>/dev/null; then
            kill "$A2A_PID" 2>/dev/null || true
            print_status "Stopped A2A Inspector (PID: $A2A_PID)"
        fi
        rm -f "$INSPECTORS_DIR/a2a_inspector.pid"
    fi

    # Also kill any stray uv/python processes for A2A inspector
    pkill -f "a2a-inspector.*app.py" 2>/dev/null || true

    print_status "All inspectors stopped"
}

# =============================================================================
# Print Summary
# =============================================================================

print_summary() {
    print_header "Inspector URLs Summary"

    echo -e "${CYAN}MCP Inspector:${NC}"
    echo "  UI:    http://localhost:$MCP_INSPECTOR_UI_PORT"
    echo ""
    echo -e "${CYAN}A2A Inspector:${NC}"
    echo "  UI:    http://localhost:$A2A_INSPECTOR_PORT"
    echo ""
    echo -e "${CYAN}MCP Server Cards:${NC}"
    echo "  Tickets MCP:   http://localhost:5401/mcp/card"
    echo "  Inquiries MCP: http://localhost:5501/mcp/card"
    echo ""
    echo -e "${CYAN}A2A Agent Cards:${NC}"
    echo "  Escalation:    http://localhost:8104/.well-known/agent-card.json"
    echo "  KB Agent:      http://localhost:8106/.well-known/agent-card.json"
    echo ""
    echo -e "${YELLOW}Note: Make sure MCP servers and A2A agents are running!${NC}"
    echo -e "${YELLOW}Run ./run_ticketflow.sh first, then use these inspectors.${NC}"
}

# =============================================================================
# Main
# =============================================================================

main() {
    mkdir -p "$INSPECTORS_DIR"

    case "${1:-all}" in
        mcp)
            launch_mcp_inspector
            wait
            ;;
        a2a)
            trap stop_inspectors EXIT INT TERM
            launch_a2a_inspector
            print_summary
            echo ""
            print_info "Press Ctrl+C to stop"
            wait
            ;;
        stop)
            stop_inspectors
            ;;
        all|"")
            print_header "TicketFlow Inspector Tools"
            print_info "Launching MCP and A2A Inspectors..."
            echo ""

            # Launch A2A Inspector first (background)
            launch_a2a_inspector

            echo ""

            # Print summary before blocking on MCP Inspector
            print_summary

            echo ""
            print_info "Launching MCP Inspector (foreground)..."
            print_info "Press Ctrl+C to stop all inspectors"
            echo ""

            # Set up trap to stop everything on exit
            trap stop_inspectors EXIT INT TERM

            # Launch MCP Inspector (foreground - will block)
            launch_mcp_inspector
            wait
            ;;
        *)
            echo "Usage: $0 [mcp|a2a|stop|all]"
            echo ""
            echo "  mcp   - Launch MCP Inspector only"
            echo "  a2a   - Launch A2A Inspector only"
            echo "  stop  - Stop all inspectors"
            echo "  all   - Launch both (default)"
            exit 1
            ;;
    esac
}

main "$@"
