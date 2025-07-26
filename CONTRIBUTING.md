# Contributing to Task List MCP Server

Thank you for your interest in contributing to the Task List MCP Server! This document provides guidelines for contributing to the project.

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Docker (for containerization)
- Git

### Development Setup
1. **Clone the repository**
   ```bash
   git clone https://github.com/P47Phoenix/Task-list-mcp.git
   cd Task-list-mcp
   ```

2. **Build the project**
   ```bash
   dotnet build
   ```

3. **Run tests** (when available)
   ```bash
   dotnet test
   ```

4. **Run the server locally**
   ```bash
   cd src/TaskListMcp.Server
   dotnet run
   ```

## Project Structure

```
src/
├── TaskListMcp.Models/      # Data models and enums
├── TaskListMcp.Data/        # Database layer (SQLite)
├── TaskListMcp.Core/        # Business logic services
└── TaskListMcp.Server/      # MCP server implementation
    ├── Tools/               # MCP tool implementations
    ├── Configuration/       # Configuration classes
    └── Controllers/         # Health check controllers
```

## Contributing Guidelines

### Code Style
- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Maintain consistent indentation (4 spaces)

### MCP Tool Development
When adding new MCP tools:

1. **Create tool class** in `src/TaskListMcp.Server/Tools/`
2. **Follow naming convention**: `[Action][Entity]Tool.cs` (e.g., `CreateTaskTool.cs`)
3. **Use static async methods** that return `McpResult<T>`
4. **Add proper error handling** and validation
5. **Update documentation** to reflect new capabilities

### Example Tool Structure
```csharp
public static class CreateTaskTool
{
    public static async Task<McpResult<object>> CreateTaskAsync(
        string title, 
        string description = "", 
        int? listId = null,
        Priority priority = Priority.Medium)
    {
        try
        {
            // Implementation
            return McpResult<object>.Success(result);
        }
        catch (Exception ex)
        {
            return McpResult<object>.Error("error_code", ex.Message);
        }
    }
}
```

### Database Schema Changes
- Add migrations for schema changes
- Ensure backward compatibility
- Update DatabaseManager initialization
- Test with existing data

### Docker Changes
- Test Docker builds locally
- Ensure multi-stage builds remain optimized
- Update health checks if needed
- Verify container functionality

## Pull Request Process

1. **Fork the repository**
2. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make your changes**
   - Follow coding standards
   - Add tests for new functionality
   - Update documentation

4. **Test your changes**
   ```bash
   dotnet build
   dotnet test
   docker build -t test-image .
   ```

5. **Commit with descriptive messages**
   ```bash
   git commit -m "Add feature: description of what was added"
   ```

6. **Push to your fork**
   ```bash
   git push origin feature/your-feature-name
   ```

7. **Create a Pull Request**
   - Provide clear description of changes
   - Link to any related issues
   - Include testing information

### Pull Request Checklist
- [ ] Code follows project style guidelines
- [ ] All tests pass
- [ ] Documentation updated
- [ ] Docker image builds successfully
- [ ] MCP tools tested with client
- [ ] Breaking changes documented

## Issue Reporting

### Bug Reports
Include:
- Steps to reproduce
- Expected vs actual behavior
- Environment details (OS, .NET version, Docker version)
- Error messages and logs
- MCP client used (Claude Desktop, VS Code, etc.)

### Feature Requests
Include:
- Clear description of the feature
- Use case or problem it solves
- Proposed implementation approach
- Impact on existing functionality

## Development Phases

The project follows a structured development approach:

### Completed Phases
- ✅ **Phase 1-7**: Core functionality through deployment

### Future Phases
- **Phase 8**: Testing and Quality Assurance
- **Phase 9**: Performance Optimization
- **Phase 10**: Advanced Features

## Code of Conduct

### Our Standards
- Use welcoming and inclusive language
- Be respectful of different viewpoints
- Focus on constructive feedback
- Show empathy towards other contributors

### Unacceptable Behavior
- Harassment or discriminatory language
- Personal attacks or trolling
- Public or private harassment
- Inappropriate use of sexualized language

## Recognition

Contributors will be recognized in:
- CONTRIBUTORS.md file
- Release notes for significant contributions
- GitHub contributor statistics

## Questions?

- Open an issue for questions about contributing
- Check existing issues and documentation first
- Provide context when asking questions

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

Thank you for contributing to the Task List MCP Server! 🎉
