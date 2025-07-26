# Task List MCP Server - Configuration Examples

This directory contains sample configurations for connecting to the Task List MCP Server Docker container (`p47phoenix/tasklist-mcp`) with various MCP clients.

## Quick Start

1. **Pull the Docker image:**
   ```bash
   docker pull p47phoenix/tasklist-mcp:latest
   ```

2. **Choose your MCP client configuration below**

## Configuration Files

### 🖥️ Claude Desktop
**File:** `claude-desktop.json`

Basic configuration for Claude Desktop application:
```json
{
  "mcpServers": {
    "tasklist": {
      "command": "docker",
      "args": ["run", "--rm", "-i", "p47phoenix/tasklist-mcp:latest"]
    }
  }
}
```

**Installation:**
- Copy content to your Claude Desktop MCP configuration file
- Location varies by OS (see Claude Desktop documentation)

### 💾 Claude Desktop (Persistent Data)
**File:** `claude-desktop-persistent.json`

Configuration with persistent data storage:
```json
{
  "mcpServers": {
    "tasklist-persistent": {
      "command": "docker",
      "args": ["run", "--rm", "-i", "-v", "./tasklist-data:/app/data", "p47phoenix/tasklist-mcp:latest"]
    }
  }
}
```

**Setup:**
1. Create data directory: `mkdir tasklist-data`
2. Use this configuration for persistent task storage

### 🆚 VS Code (Cline Extension)
**File:** `cline-vscode.json`

Configuration for the Cline VS Code extension:
```json
{
  "cline.mcpServers": {
    "tasklist": {
      "command": "docker",
      "args": ["run", "--rm", "-i", "p47phoenix/tasklist-mcp:latest"]
    }
  }
}
```

**Installation:**
1. Install the Cline extension in VS Code
2. Add this configuration to your VS Code settings.json

### 🐳 Docker Compose
**File:** `docker-compose.yml`

Full-featured deployment with health monitoring and backup:
```bash
# Start the service
docker-compose up -d

# Check logs
docker-compose logs -f tasklist-mcp

# Stop the service
docker-compose down
```

**Features:**
- Persistent data storage
- Health check monitoring
- Automatic database backups
- Network isolation

### 🖱️ Universal Configuration
**Files:** `universal-config.sh` (Linux/macOS), `windows-config.ps1` (Windows)

Command-line examples for any MCP client:

**Linux/macOS:**
```bash
docker run --rm -i --name tasklist-mcp p47phoenix/tasklist-mcp:latest
```

**Windows PowerShell:**
```powershell
docker run --rm -i --name tasklist-mcp p47phoenix/tasklist-mcp:latest
```

## Configuration Options

### Environment Variables
- `ASPNETCORE_ENVIRONMENT`: Set to `Development` or `Production`
- `DATABASE_PATH`: Custom database file path (default: `/app/data/tasks.db`)

### Volume Mounts
- Mount `/app/data` for persistent database storage
- Example: `-v ./tasklist-data:/app/data`

### Port Mappings (Optional)
- `8080`: Health check endpoint
- `8081`: Additional services
- Example: `-p 8080:8080`

## Usage Examples

### Basic Usage (Temporary Data)
```bash
docker run -it --rm p47phoenix/tasklist-mcp:latest
```

### With Persistent Data
```bash
# Create data directory
mkdir tasklist-data

# Run with persistent storage
docker run -it --rm -v $(pwd)/tasklist-data:/app/data p47phoenix/tasklist-mcp:latest
```

### With Health Monitoring
```bash
# Run in background with health endpoint
docker run -d -p 8080:8080 --name tasklist-mcp p47phoenix/tasklist-mcp:latest

# Check health
curl http://localhost:8080/health

# Connect to MCP server
docker exec -it tasklist-mcp /bin/bash
```

## MCP Client Compatibility

This container works with any MCP client that supports:
- ✅ Standard MCP protocol
- ✅ Command-line server execution
- ✅ Docker container integration
- ✅ Environment variable configuration

### Tested Clients
- Claude Desktop
- VS Code Cline extension
- Custom MCP clients
- Command-line MCP tools

## Troubleshooting

### Common Issues

1. **Container exits immediately**
   - Ensure `-i` flag is used for interactive mode
   - Check Docker daemon is running

2. **Permission errors**
   - Ensure mounted directories have correct permissions
   - Try running with `--user $(id -u):$(id -g)` on Linux

3. **Database not persisting**
   - Verify volume mount path: `-v ./tasklist-data:/app/data`
   - Check directory exists and is writable

4. **Health check fails**
   - Container may need time to start (30-40 seconds)
   - Verify port mapping: `-p 8080:8080`

### Debug Commands
```bash
# Check container logs
docker logs <container-name>

# Connect to running container
docker exec -it <container-name> /bin/bash

# Test MCP connection manually
echo '{"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {"protocolVersion": "2024-11-05", "capabilities": {}, "clientInfo": {"name": "test", "version": "1.0.0"}}}' | docker run -i --rm p47phoenix/tasklist-mcp:latest
```

## Support

- **Docker Hub**: https://hub.docker.com/r/p47phoenix/tasklist-mcp
- **GitHub Repository**: https://github.com/P47Phoenix/Task-list-mcp
- **Documentation**: See main README.md
- **Issues**: Report issues at https://github.com/P47Phoenix/Task-list-mcp/issues

## Features Available

Once connected, you'll have access to **34 MCP tools**:
- 8 Task Management tools
- 5 List Management tools
- 5 Template Management tools
- 5 Tag Management tools
- 7 Attribute Management tools
- 4 Search and Analytics tools

Start with `list_all_lists` to see your task lists or `create_list` to create your first list!
