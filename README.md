# Task List MCP Server

A comprehensive Model Context Protocol (MCP) server for managing hierarchical task lists with templates, tagging, and advanced features.

## Features

- **Hierarchical Task Management**: Create tasks with subtasks and organize them in nested lists
- **Single Active Task Constraint**: Only one task per list can be "in progress" at a time
- **Template System**: Create reusable templates from existing task lists
- **Advanced Tagging**: Tag both tasks and lists with hierarchical tag support
- **Custom Attributes**: Add custom fields to tasks and lists
- **SQLite Database**: Persistent storage with automatic database creation
- **Rich Filtering**: Search and filter tasks by status, priority, tags, dates, and more

## Installation

### Docker Hub (Recommended)

The easiest way to use the Task List MCP Server is via the pre-built Docker container:

```bash
# Pull the container
docker pull p47phoenix/tasklist-mcp:latest

# Run with temporary data
docker run -it --rm p47phoenix/tasklist-mcp:latest

# Run with persistent data
mkdir tasklist-data
docker run -it --rm -v $(pwd)/tasklist-data:/app/data p47phoenix/tasklist-mcp:latest
```

### MCP Client Configuration

Ready-to-use configurations for popular MCP clients are available in the `mcp-configs/` directory:

- **Claude Desktop**: `mcp-configs/claude-desktop.json`
- **VS Code Cline**: `mcp-configs/cline-vscode.json`
- **Docker Compose**: `mcp-configs/docker-compose.yml`
- **Universal**: `mcp-configs/universal-config.sh`

See [mcp-configs/README.md](mcp-configs/README.md) for detailed setup instructions.

### Build from Source

1. **Quick Deploy**
   ```bash
   # Linux/macOS
   chmod +x deploy.sh
   ./deploy.sh

   # Windows
   deploy.bat
   ```

2. **Manual Docker**
   ```bash
   docker-compose up -d --build
   ```

### Local Development

1. **Prerequisites**
   - .NET 8.0 SDK

2. **Build and Run**
   ```bash
   dotnet build
   cd src/TaskListMcp.Server
   dotnet run
   ```

## Usage

### Production Deployment

See [DEPLOYMENT.md](DEPLOYMENT.md) for comprehensive deployment options including:
- Docker and Docker Compose
- Kubernetes with auto-scaling  
- Cloud platforms (Azure, AWS, Google Cloud)
- Monitoring and health checks
- Security configurations

### Health Monitoring

- **Basic Health**: `http://localhost:8080/health`
- **Detailed Metrics**: `http://localhost:8080/health/detailed`

### Database Location

By default, the database is created as `tasks.db` in the current directory. You can specify a custom path using the `TASKLIST_DB_PATH` environment variable.

## Available Tools

### Task Management

- `create_task` - Create a new task
- `update_task` - Update an existing task
- `delete_task` - Delete a task
- `get_task` - Get task details
- `list_tasks` - List tasks with filtering options
- `start_task` - Start working on a task (sets status to in_progress)
- `complete_task` - Mark a task as completed
- `move_task` - Move a task to a different list

### List Management

- `create_list` - Create a new task list
- `update_list` - Update a task list
- `delete_list` - Delete a task list
- `get_list` - Get list details with tasks
- `list_all_lists` - Get all task lists

### Template Management

- `create_template` - Create a template from an existing task list
- `apply_template` - Create a task list from a template
- `list_templates` - List available templates
- `get_template` - Get template details
- `delete_template` - Delete a template

### Search and Analytics

- `search_tasks` - Advanced task search with filtering by status, priority, tags, dates
- `search_lists` - Search task lists with comprehensive filtering options
- `get_search_suggestions` - Get search suggestions based on existing data
- `get_task_analytics` - Get detailed analytics and insights about tasks

### Health and Monitoring

- Health check endpoints (`/health`, `/health/detailed`)
- Database connectivity monitoring
- Performance metrics and system information
- Docker health checks and container monitoring

## Available Resources

- `tasks://all` - Complete list of all tasks with details
- `lists://all` - Complete list of all task lists with details
- `templates://all` - Complete list of all templates
- `dashboard://overview` - Overview dashboard with metrics

## Key Features in Detail

### Single Active Task Constraint

Each task list can only have one task with status "in_progress" at a time. When you start a new task, any previously active task in the same list is automatically set to "pending".

### Template System

Templates preserve the structure and content of task lists but strip away all state information (status, completion dates, etc.). When applying a template, you can:

- Create a new list from the template
- Add tasks to an existing list
- Use parameter substitution with `{{placeholder}}` syntax

### Hierarchical Organization

- **Nested Lists**: Lists can contain sub-lists for organizing complex projects
- **Subtasks**: Tasks can have subtasks for breaking down work
- **Tagging**: Both tasks and lists support multiple tags
- **Custom Attributes**: Add any custom fields you need

### Advanced Filtering

Filter tasks by:
- Status (pending, in_progress, completed, cancelled, blocked)
- Priority (low, medium, high, critical)
- Tags
- Due dates
- List membership
- Text search in title/description

## Database Schema

The server uses SQLite with the following main tables:

- `tasks` - Individual tasks with status, priority, dates
- `task_lists` - Task list containers with hierarchical support
- `templates` - Template definitions
- `template_tasks` - Tasks within templates
- `tags` - Tag definitions
- `task_tags` / `list_tags` - Tag associations
- `attributes` - Custom attribute definitions
- `task_attributes` / `list_attributes` - Attribute values

## Development

### Build
```bash
npm run build
```

### Watch mode
```bash
npm run dev
```

### Linting
```bash
npm run lint
```

### Formatting
```bash
npm run format
```

## Implementation Status

### Core Features
- ✅ **Phase 1: Core Task Management** - Basic CRUD operations for tasks and lists (16 tools)
- ✅ **Phase 2: Advanced Task Operations** - Status management, task moving, completion tracking
- ✅ **Phase 3: Resources and Data Access** - MCP resources for data retrieval
- ✅ **Phase 4: Template System** - Template creation, application, and management (5 tools)
- ✅ **Phase 5: Tagging and Attributes** - Complete tagging system with hierarchy and custom attributes (15 tools)
  - ✅ Hierarchical Tagging System (5 tools)
  - ✅ Custom Attributes System (8 tools)
- ✅ **Phase 6: Advanced Features** - Search, filtering, analytics (4 tools)
  - ✅ Advanced Search and Filtering (2 tools)
  - ✅ Search Suggestions and Analytics (2 tools)
- ✅ **Phase 7: Configuration and Deployment** - Docker, environment setup, health monitoring
  - ✅ Docker containerization with multi-stage builds
  - ✅ Kubernetes deployment configurations
  - ✅ Health check endpoints and monitoring
  - ✅ Environment-based configuration management
  - ✅ Production-ready deployment scripts

### MCP Tools Summary
**Total Tools: 34**
- Task Management: 8 tools
- List Management: 5 tools  
- Template Management: 5 tools
- Tag Management: 5 tools
- Attribute Management: 7 tools
- Search and Analytics: 4 tools

## Configuration

The server can be configured by modifying the initialization parameters in `src/index.ts`. Key configuration options include:

- Database file path
- Default list creation
- Validation rules
- Performance settings

## Error Handling

The server includes comprehensive error handling for:

- Invalid task operations (e.g., starting multiple tasks)
- Missing resources
- Database constraints
- Circular references in hierarchical structures

## Performance Considerations

- Database indexes on frequently queried columns
- Transaction support for complex operations
- Lazy loading of related data
- Configurable query limits

## Future Enhancements

Potential areas for expansion:
- Real-time notifications
- Collaboration features
- Time tracking
- Reporting and analytics
- Integration with external calendars
- Mobile app support
- Plugin system

## License

MIT License - see LICENSE file for details.
