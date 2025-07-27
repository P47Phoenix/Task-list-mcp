#!/usr/bin/env pwsh
# GitHub Release Creator for Task List MCP Server v1.1.0
# This script creates a comprehensive GitHub release

param(
    [switch]$DryRun = $false
)

$ErrorActionPreference = "Stop"

# Colors for output
$Green = "`e[32m"
$Blue = "`e[34m"
$Yellow = "`e[33m"
$Red = "`e[31m"
$Reset = "`e[0m"

function Write-ColorOutput {
    param($Color, $Message)
    Write-Host "$Color$Message$Reset"
}

Write-ColorOutput $Blue "🚀 Task List MCP Server - GitHub Release Creator"
Write-ColorOutput $Blue "================================================"
Write-Host ""

# Release information
$version = "v1.1.0"
$title = "Task List MCP Server v1.1.0 - Testing Infrastructure & Docker Hub Publishing"
$tag = "v1.1.0"

# Check if tag exists
try {
    $tagExists = git tag -l $tag
    if (-not $tagExists) {
        Write-ColorOutput $Red "❌ Error: Tag $tag does not exist"
        Write-ColorOutput $Yellow "Please create the tag first: git tag -a $tag -m 'Release message'"
        exit 1
    }
    Write-ColorOutput $Green "✅ Tag $tag exists"
} catch {
    Write-ColorOutput $Red "❌ Error: Failed to check tag existence"
    exit 1
}

# Check GitHub CLI authentication
Write-ColorOutput $Blue "🔐 Checking GitHub CLI authentication..."
try {
    gh auth status 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput $Yellow "⚠️  GitHub CLI not authenticated"
        Write-ColorOutput $Blue "Please authenticate with: gh auth login"
        
        $authenticate = Read-Host "Authenticate now? (y/N)"
        if ($authenticate -match "^[yY]$") {
            gh auth login
            if ($LASTEXITCODE -ne 0) {
                Write-ColorOutput $Red "❌ Authentication failed"
                exit 1
            }
        } else {
            Write-ColorOutput $Yellow "Manual authentication required. Run: gh auth login"
            exit 1
        }
    }
    Write-ColorOutput $Green "✅ GitHub CLI authenticated"
} catch {
    Write-ColorOutput $Red "❌ Error: GitHub CLI authentication check failed"
    exit 1
}

# Release notes
$releaseNotes = @"
## 🎉 Release Highlights

This release adds comprehensive testing infrastructure and Docker Hub publishing capabilities to the Task List MCP Server, making it production-ready and easily deployable.

## ✨ New Features

### 🧪 Comprehensive Testing Infrastructure
- **Unit Testing Framework**: Complete xUnit-based unit testing with 16 passing tests
- **Integration Testing**: Ready-to-use integration test infrastructure with Testcontainers
- **Performance Testing**: BenchmarkDotNet and NBomber setup for load testing
- **Code Coverage**: Coverlet integration with detailed reporting
- **Test Automation**: PowerShell and Bash test runners with CI/CD support

### 🐳 Docker Hub Publishing
- **Production Docker Images**: Available on Docker Hub as ``p47phoenix/tasklist-mcp``
- **Multi-Stage Builds**: Optimized Dockerfile with production-only dependencies
- **Automated Publishing**: Interactive PowerShell script for easy Docker Hub deployment
- **Security**: Non-root user execution and proper container hardening

### 🚀 CI/CD Pipeline
- **GitHub Actions**: Ready-to-use workflows for testing and deployment
- **Automated Testing**: Unit, integration, and Docker container tests
- **Coverage Reporting**: Automated code coverage generation and reporting
- **Release Management**: Proper semantic versioning and tag management

## 📦 Docker Hub Deployment

The Task List MCP Server is now available on Docker Hub:

````bash
# Latest version
docker pull p47phoenix/tasklist-mcp:latest

# Specific version
docker pull p47phoenix/tasklist-mcp:1.1.0

# Run the container
docker run -d -p 8080:8080 -v tasklist_data:/app/data p47phoenix/tasklist-mcp:latest
````

### Docker Compose Example
````yaml
version: '3.8'
services:
  tasklist-mcp:
    image: p47phoenix/tasklist-mcp:1.1.0
    ports:
      - "8080:8080"
    volumes:
      - tasklist_data:/app/data
    environment:
      - TASKLIST_DB_PATH=/app/data/tasks.db
      - ASPNETCORE_ENVIRONMENT=Production
    restart: unless-stopped

volumes:
  tasklist_data:
````

## 🔧 Technical Improvements

### Testing Infrastructure
- **Test Projects**: 4 dedicated test projects (Unit, Integration, Performance, Docker)
- **Test Dependencies**: xUnit, Moq, FluentAssertions, AutoFixture, BenchmarkDotNet
- **Database Testing**: SQLite with temporary file-based testing approach
- **Mock Framework**: Comprehensive mock factory for dependency injection testing
- **Test Data Builders**: Fluent API for creating test data scenarios

### Docker Enhancements
- **Production Solution**: Separate solution file excluding test projects for faster builds
- **Health Checks**: Built-in health check endpoint at ``/health``
- **Data Persistence**: Proper volume mounting for SQLite database
- **Environment Configuration**: Comprehensive environment variable support
- **Security**: AppUser implementation for non-root execution

## 🐛 Bug Fixes

- Fixed Dockerfile to exclude test projects from production builds
- Resolved dependency conflicts in test projects
- Improved database connection management in testing scenarios

## 📊 Test Coverage

- **Unit Tests**: 16/16 passing (100% pass rate)
- **Code Coverage**: Configured for Core, Data, and Server projects
- **Performance Benchmarks**: Ready for load testing scenarios
- **Integration Tests**: Infrastructure prepared for end-to-end testing

## 🛠️ Breaking Changes

None. This release is fully backward compatible with v1.0.1.

## 🚀 Getting Started

### Quick Run with Docker
````bash
docker run -d -p 8080:8080 p47phoenix/tasklist-mcp:1.1.0
````

### Development Setup
````bash
git clone https://github.com/P47Phoenix/Task-list-mcp.git
cd Task-list-mcp
dotnet restore
dotnet test  # Run all tests
dotnet run --project src/TaskListMcp.Server
````

## 🔗 Links

- **Docker Hub**: https://hub.docker.com/r/p47phoenix/tasklist-mcp
- **Documentation**: See ``docs/`` folder for comprehensive guides
- **Issues**: Report bugs on GitHub Issues

**Full Changelog**: https://github.com/P47Phoenix/Task-list-mcp/compare/v1.0.1...v1.1.0
"@

if ($DryRun) {
    Write-ColorOutput $Yellow "🔍 DRY RUN MODE - No actual release will be created"
    Write-Host ""
    Write-ColorOutput $Blue "Release Details:"
    Write-Host "Title: $title"
    Write-Host "Tag: $tag"
    Write-Host "Notes Length: $($releaseNotes.Length) characters"
    Write-Host ""
    Write-ColorOutput $Yellow "Command that would be executed:"
    Write-Host "gh release create $tag --title `"$title`" --notes `"$releaseNotes`""
    exit 0
}

# Create the release
Write-ColorOutput $Blue "🎯 Creating GitHub release..."
Write-Host ""
Write-ColorOutput $Blue "Title: $title"
Write-ColorOutput $Blue "Tag: $tag"
Write-Host ""

try {
    # Create release with notes
    gh release create $tag --title $title --notes $releaseNotes
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput $Green "🎉 GitHub release created successfully!"
        Write-Host ""
        Write-ColorOutput $Blue "📋 Release Details:"
        Write-ColorOutput $Blue "   • Version: $version"
        Write-ColorOutput $Blue "   • Tag: $tag"
        Write-ColorOutput $Blue "   • Docker Images: p47phoenix/tasklist-mcp:latest, p47phoenix/tasklist-mcp:1.1.0"
        Write-Host ""
        Write-ColorOutput $Blue "🔗 View release: https://github.com/P47Phoenix/Task-list-mcp/releases/tag/$tag"
    } else {
        Write-ColorOutput $Red "❌ Failed to create GitHub release"
        exit 1
    }
} catch {
    Write-ColorOutput $Red "❌ Error creating GitHub release: $_"
    exit 1
}

Write-Host ""
Write-ColorOutput $Green "✅ Release creation completed successfully!"
