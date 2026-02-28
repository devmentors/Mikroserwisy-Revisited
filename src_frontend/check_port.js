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
