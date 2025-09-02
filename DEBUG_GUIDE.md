# Debugging Chapi.API in VS Code

## Prerequisites

1. **Install Required Extensions**
   - Open the Command Palette (`Cmd+Shift+P`)
   - Run `Extensions: Show Recommended Extensions`
   - Install all recommended extensions (especially C# Dev Kit)

2. **Database Setup (Optional for basic debugging)**
   - PostgreSQL is required for full functionality
   - For basic API debugging without DB, see "Debug without Database" section below

## Quick Start Debugging

### Method 1: Using VS Code Debug Panel

1. **Open the project** in VS Code (make sure you're in the root `/chapi` folder)

2. **Go to Debug Panel**
   - Click the Debug icon in the left sidebar (or press `Cmd+Shift+D`)

3. **Select Configuration**
   - Choose "Launch Chapi.API" from the dropdown
   - Click the green play button or press `F5`

4. **Set Breakpoints**
   - Click in the left margin of any `.cs` file to set breakpoints
   - The debugger will pause at breakpoints during execution

### Method 2: Using Command Palette

1. Press `Cmd+Shift+P`
2. Type "Debug: Start Debugging" and select it
3. Choose "Launch Chapi.API" configuration

## Debug Configurations Available

- **Launch Chapi.API**: Standard HTTP debugging (port 5066)
- **Launch Chapi.API (HTTPS)**: HTTPS debugging (port 7199)
- **Attach to Chapi.API**: Attach to already running process

## Debugging Features

### Breakpoints

- **Line Breakpoints**: Click in the left margin
- **Conditional Breakpoints**: Right-click on breakpoint → Edit Breakpoint
- **Logpoints**: Right-click in margin → Add Logpoint

### Debug Controls

- **Continue** (`F5`): Resume execution
- **Step Over** (`F10`): Execute current line
- **Step Into** (`F11`): Step into function calls
- **Step Out** (`Shift+F11`): Step out of current function
- **Restart** (`Cmd+Shift+F5`): Restart debugging session
- **Stop** (`Shift+F5`): Stop debugging

### Watch and Variables

- **Variables Panel**: View local variables and their values
- **Watch Panel**: Add expressions to monitor
- **Call Stack**: See the execution path
- **Debug Console**: Execute expressions during debugging

## Debug without Database

If PostgreSQL isn't running, you can still debug basic functionality:

1. **Comment out database operations** in:
   - `DatabaseModule.cs` (temporarily skip migrations)
   - Or use in-memory database for testing

2. **Focus on API endpoints** that don't require database:
   - Health checks
   - Static endpoints
   - Authentication flows (if using external providers)

## Common Debugging Scenarios

### Debug a Specific Controller

1. Set breakpoint in the controller method
2. Start debugging
3. Make HTTP request to the endpoint (using Postman, curl, or browser)
4. Debugger will pause at your breakpoint

### Debug Dependency Injection

1. Set breakpoints in module configuration files
2. Check service registrations during startup
3. Use the Variables panel to inspect service collections

### Debug Semantic Kernel Integration

1. Set breakpoints in `ChapiAIModule.cs` or `RunPackService.cs`
2. Test AI-related endpoints
3. Monitor kernel operations and plugin executions

## Troubleshooting

### Build Errors

- Run `Ctrl+Shift+P` → "Tasks: Run Task" → "build" to see detailed errors
- Check the Problems panel for compilation issues

### Debugging Not Starting

1. Ensure all extensions are installed
2. Try rebuilding: `Ctrl+Shift+P` → "Tasks: Run Task" → "clean" then "build"
3. Check the Debug Console for error messages

### Database Connection Issues

- Start PostgreSQL service
- Verify connection string in `appsettings.Development.json`
- Or temporarily disable database operations for basic debugging

## Useful VS Code Shortcuts

- `F5`: Start/Continue debugging
- `F9`: Toggle breakpoint
- `F10`: Step over
- `F11`: Step into
- `Cmd+Shift+F5`: Restart debugging
- `Cmd+K Cmd+I`: Show hover information
- `Cmd+.`: Show code actions
- `Cmd+Shift+P`: Command palette

## Tips for Effective Debugging

1. **Use conditional breakpoints** for loops or frequently called methods
2. **Add logpoints** instead of `Console.WriteLine` for temporary logging
3. **Use the Debug Console** to evaluate expressions without modifying code
4. **Monitor the Call Stack** to understand execution flow
5. **Use the Variables panel** to inspect object states
6. **Set breakpoints in exception handlers** to catch and analyze errors

Happy debugging! 🐛🔍
