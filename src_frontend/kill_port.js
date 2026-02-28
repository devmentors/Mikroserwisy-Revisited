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
