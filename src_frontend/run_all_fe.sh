#!/bin/bash

# API mode: gateway (default), bff, direct
API_MODE=${1:-gateway}
export NEXT_PUBLIC_API_MODE=$API_MODE

cleanup() {
    echo ""
    echo "Stopping all frontend services..."
    pkill -f "next-server" 2>/dev/null
    pkill -f "next dev" 2>/dev/null
    rm -f check_port.js kill_port.js 2>/dev/null
    echo "All frontend services stopped"
    exit 0
}

trap cleanup SIGINT SIGTERM

echo "============================================"
echo "Starting frontend apps in '$API_MODE' mode"
echo "============================================"
echo ""

# Create a temporary Node.js script for killing processes
cat > kill_port.js << 'EOF'
const { execSync } = require('child_process');
const port = process.argv[2];

try {
    if (process.platform === 'win32') {
        const command = `for /f "tokens=5" %a in ('netstat -aon ^| findstr :${port}') do taskkill /F /PID %a 2>NUL`;
        execSync(command, { shell: 'cmd.exe', stdio: 'ignore' });
    } else {
        execSync(`lsof -i :${port} -t | xargs -r kill -9`, { stdio: 'ignore' });
    }
} catch (error) {
    // Ignore errors if no process is found
}
EOF

# Function to kill process using a specific port
kill_port() {
    local port=$1
    echo "Checking if port $port is in use..."
    node kill_port.js $port
}

# Kill existing processes
echo "Cleaning up existing processes..."
kill_port 21000
kill_port 21001
kill_port 21002
kill_port 21003

# Install dependencies in parallel
echo "Installing dependencies..."
(cd ./inquiries/ && npm install) &
(cd ./tickets/ && npm install) &
(cd ./technical/ && npm install) &
(cd ./dashboard/ && npm install) &
wait

# Create a temporary Node.js script for port checking
cat > check_port.js << 'EOF'
const net = require('net');
const port = process.argv[2];

function checkPort() {
    const socket = new net.Socket();
    
    socket.on('connect', () => {
        socket.destroy();
        process.exit(0);
    });
    
    socket.on('error', (err) => {
        socket.destroy();
        setTimeout(checkPort, 1000);
    });

    socket.connect(port, 'localhost');
}

checkPort();
EOF

# Function to wait for a port to be available
wait_for_port() {
    local port=$1
    echo "Waiting for port $port..."
    node check_port.js $port
}

# Function to warmup a Next.js application
warmup_app() {
    local port=$1
    echo "Warming up application on port $port..."
    curl -sSL "http://localhost:$port" > /dev/null 2>&1 || wget -q -O /dev/null "http://localhost:$port"
}

# Start dev servers in background
echo "Starting development servers..."
(cd ./inquiries/ && npm run dev) &
(cd ./tickets/ && npm run dev) &
(cd ./technical/ && npm run dev) &
(cd ./dashboard/ && npm run dev) &

# Wait for ports to be available and warmup each application
echo "Waiting for servers to be ready and warming up..."
wait_for_port 21000 && warmup_app 21000 &
wait_for_port 21001 && warmup_app 21001 &
wait_for_port 21002 && warmup_app 21002 &
wait_for_port 21003 && warmup_app 21003 &

# Wait for all warmup processes to complete
wait

# Clean up temporary files
rm check_port.js
rm kill_port.js

echo "All applications are running and warmed up!"
