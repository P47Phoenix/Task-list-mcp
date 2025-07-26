# Quick Start with MCP Clients

This guide shows you how to quickly connect to the Task List MCP Server using popular MCP clients.

## 🚀 For Claude Desktop Users

1. **Pull the container:**
   ```bash
   docker pull p47phoenix/tasklist-mcp:latest
   ```

2. **Add to Claude Desktop configuration:**
   ```json
   {
     "mcpServers": {
       "tasklist": {
         "command": "docker",
         "args": [
           "run", "--rm", "-i", 
           "--name", "tasklist-mcp-claude",
           "p47phoenix/tasklist-mcp:latest"
         ]
       }
     }
   }
   ```

3. **For persistent data storage:**
   ```bash
   mkdir tasklist-data
   ```
   
   Then use the configuration from `mcp-configs/claude-desktop-persistent.json`

## 🆚 For VS Code Cline Extension

1. **Install the Cline extension in VS Code**

2. **Add to your VS Code settings.json:**
   ```json
   {
     "cline.mcpServers": {
       "tasklist": {
         "command": "docker",
         "args": [
           "run", "--rm", "-i",
           "--name", "cline-tasklist-mcp", 
           "p47phoenix/tasklist-mcp:latest"
         ]
       }
     }
   }
   ```

## 🐳 For Docker Compose Users

1. **Download the docker-compose.yml:**
   ```bash
   curl -O https://raw.githubusercontent.com/p47phoenix/tasklist-mcp/main/mcp-configs/docker-compose.yml
   ```

2. **Start the services:**
   ```bash
   docker-compose up -d
   ```

3. **Connect your MCP client to:**
   ```bash
   docker exec -it tasklist-mcp-service
   ```

## 🎯 First Steps

Once connected to any MCP client:

1. **Create your first list:**
   ```
   Use the create_list tool with name "My Tasks"
   ```

2. **Add a task:**
   ```
   Use create_task tool with title "Get started with Task List MCP"
   ```

3. **Explore available tools:**
   ```
   Your MCP client will show 34 available tools for task management
   ```

## 📚 Available Tools

- **Task Management**: create_task, update_task, list_tasks, start_task, complete_task, etc.
- **List Management**: create_list, update_list, get_list, list_all_lists, etc.
- **Templates**: create_template, apply_template, list_templates, etc.
- **Search & Analytics**: search_tasks, search_lists, get_task_analytics, etc.
- **Tags & Attributes**: Full tagging and custom attribute systems

## 🔧 Troubleshooting

- **Container exits immediately**: Ensure you're using the `-i` flag for interactive mode
- **Data not persisting**: Use volume mounts like `-v ./tasklist-data:/app/data`
- **Permission issues**: Check directory permissions for mounted volumes

For detailed configuration options, see [mcp-configs/README.md](mcp-configs/README.md)
