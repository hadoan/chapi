# VS Code Configuration

This folder contains shared VS Code settings for the Chapi project to ensure consistent development experience across the team.

## Files:

- **`launch.json`**: Debug configurations for Chapi.API
- **`tasks.json`**: Build and run tasks
- **`settings.json`**: Project-specific editor settings
- **`extensions.json`**: Recommended VS Code extensions

## Usage:

1. Open the project in VS Code
2. Install recommended extensions when prompted
3. Use `F5` to start debugging with the default configuration
4. Select different debug configurations from the Debug panel dropdown

## Debug Configurations:

- **Launch Chapi.API (HTTP - Port 5066)**: Default HTTP debugging
- **Launch Chapi.API (HTTPS - Port 7199)**: HTTPS debugging  
- **Launch Chapi.API (Skip DB)**: Debug without database dependency

These configurations are shared to help new team members get started quickly with debugging.
