#!/bin/bash
# Universal MCP Client Configuration Script
# This script demonstrates how to use the Task List MCP Server container

# Configuration for any MCP client that accepts command-line arguments
MCP_SERVER_COMMAND="docker run --rm -i --name tasklist-mcp-universal p47phoenix/tasklist-mcp:latest"

echo "Task List MCP Server Command:"
echo "$MCP_SERVER_COMMAND"
echo ""
echo "Environment Variables:"
echo "  ASPNETCORE_ENVIRONMENT=Production"
echo "  DATABASE_PATH=/app/data/tasks.db (optional)"
echo ""
echo "For persistent data, add volume mount:"
echo "  -v \$(pwd)/tasklist-data:/app/data"
echo ""
echo "Example usage with any MCP client:"
echo "  mcp-client --server-command=\"$MCP_SERVER_COMMAND\""
