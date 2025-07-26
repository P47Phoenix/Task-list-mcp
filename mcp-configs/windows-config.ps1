# PowerShell Configuration for Windows MCP Clients
# This script provides configuration examples for Windows users

# Basic Configuration
$McpServerCommand = @(
    "docker", "run", "--rm", "-i", 
    "--name", "tasklist-mcp-windows",
    "p47phoenix/tasklist-mcp:latest"
)

# With Persistent Data (Windows paths)
$McpServerCommandPersistent = @(
    "docker", "run", "--rm", "-i",
    "--name", "tasklist-mcp-persistent-windows",
    "-v", "${PWD}\tasklist-data:/app/data",
    "p47phoenix/tasklist-mcp:latest"
)

Write-Host "Task List MCP Server Configuration for Windows"
Write-Host "=============================================="
Write-Host ""
Write-Host "Basic Command:"
Write-Host ($McpServerCommand -join " ")
Write-Host ""
Write-Host "With Persistent Data:"
Write-Host ($McpServerCommandPersistent -join " ")
Write-Host ""
Write-Host "Environment Variables:"
Write-Host "  ASPNETCORE_ENVIRONMENT=Production"
Write-Host "  DATABASE_PATH=/app/data/tasks.db"
Write-Host ""
Write-Host "To create data directory:"
Write-Host "  New-Item -ItemType Directory -Force -Path tasklist-data"
