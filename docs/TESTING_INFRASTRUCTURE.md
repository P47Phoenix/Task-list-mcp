# Testing Infrastructure Setup Guide

## Overview
This guide provides detailed instructions for setting up the testing infrastructure for the Task List MCP Server project, including test projects, dependencies, configuration, and automation.

## Project Structure

### Test Project Organization
```
tests/
├── TaskListMcp.Tests.Unit/           # Unit tests for individual components
│   ├── Services/                     # Service layer tests
│   │   ├── TaskServiceTests.cs
│   │   ├── ListServiceTests.cs
│   │   ├── TemplateServiceTests.cs
│   │   ├── TagServiceTests.cs
│   │   ├── AttributeServiceTests.cs
│   │   └── SearchServiceTests.cs
│   ├── Tools/                        # MCP tool tests
│   │   ├── TaskToolsTests.cs
│   │   ├── ListToolsTests.cs
│   │   ├── TemplateToolsTests.cs
│   │   └── TagAttributeToolsTests.cs
│   ├── Models/                       # Model validation tests
│   │   ├── TaskTests.cs
│   │   ├── TaskListTests.cs
│   │   └── ValidationTests.cs
│   └── Helpers/                      # Test utility classes
│       ├── TestDataBuilder.cs
│       ├── MockFactory.cs
│       └── TestFixture.cs
├── TaskListMcp.Tests.Integration/    # Integration tests
│   ├── Database/                     # Database integration tests
│   │   ├── TransactionTests.cs
│   │   ├── ConcurrencyTests.cs
│   │   └── SchemaTests.cs
│   ├── Services/                     # Service integration tests
│   │   ├── TaskListIntegrationTests.cs
│   │   ├── TemplateIntegrationTests.cs
│   │   └── SearchIntegrationTests.cs
│   ├── Workflows/                    # End-to-end workflow tests
│   │   ├── TaskLifecycleTests.cs
│   │   ├── ProjectManagementTests.cs
│   │   └── HierarchyManagementTests.cs
│   └── Configuration/
│       ├── appsettings.test.json
│       └── TestDbFixture.cs
├── TaskListMcp.Tests.Performance/    # Performance and load tests
│   ├── LoadTests.cs
│   ├── BenchmarkTests.cs
│   ├── MemoryTests.cs
│   └── ConcurrencyTests.cs
└── TaskListMcp.Tests.Docker/         # Docker-specific tests
    ├── ContainerTests.cs
    ├── HealthCheckTests.cs
    └── DeploymentTests.cs
```

## Setting Up Test Projects

### 1. Create Test Projects

```powershell
# Navigate to the solution root
cd c:\GitHub\Task-list-mcp

# Create test projects
dotnet new classlib -n TaskListMcp.Tests.Unit -o tests/TaskListMcp.Tests.Unit
dotnet new classlib -n TaskListMcp.Tests.Integration -o tests/TaskListMcp.Tests.Integration
dotnet new classlib -n TaskListMcp.Tests.Performance -o tests/TaskListMcp.Tests.Performance
dotnet new classlib -n TaskListMcp.Tests.Docker -o tests/TaskListMcp.Tests.Docker

# Add projects to solution
dotnet sln add tests/TaskListMcp.Tests.Unit/TaskListMcp.Tests.Unit.csproj
dotnet sln add tests/TaskListMcp.Tests.Integration/TaskListMcp.Tests.Integration.csproj
dotnet sln add tests/TaskListMcp.Tests.Performance/TaskListMcp.Tests.Performance.csproj
dotnet sln add tests/TaskListMcp.Tests.Docker/TaskListMcp.Tests.Docker.csproj
```

### 2. Configure Test Dependencies

#### Unit Test Project (TaskListMcp.Tests.Unit.csproj)
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.4" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.6">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Moq" Version="4.20.69" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Data.Sqlite.Core" Version="8.0.1" />
    <PackageReference Include="AutoFixture" Version="4.18.1" />
    <PackageReference Include="AutoFixture.Xunit2" Version="4.18.1" />
    <PackageReference Include="coverlet.collector" Version="6.0.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\TaskListMcp.Core\TaskListMcp.Core.csproj" />
    <ProjectReference Include="..\..\src\TaskListMcp.Data\TaskListMcp.Data.csproj" />
    <ProjectReference Include="..\..\src\TaskListMcp.Models\TaskListMcp.Models.csproj" />
    <ProjectReference Include="..\..\src\TaskListMcp.Server\TaskListMcp.Server.csproj" />
  </ItemGroup>

</Project>
```

#### Integration Test Project (TaskListMcp.Tests.Integration.csproj)
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.4" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.6">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.1" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.1" />
    <PackageReference Include="Testcontainers" Version="3.7.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\TaskListMcp.Core\TaskListMcp.Core.csproj" />
    <ProjectReference Include="..\..\src\TaskListMcp.Data\TaskListMcp.Data.csproj" />
    <ProjectReference Include="..\..\src\TaskListMcp.Models\TaskListMcp.Models.csproj" />
    <ProjectReference Include="..\..\src\TaskListMcp.Server\TaskListMcp.Server.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="appsettings.test.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

#### Performance Test Project (TaskListMcp.Tests.Performance.csproj)
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.4" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.6">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="BenchmarkDotNet" Version="0.13.12" />
    <PackageReference Include="NBomber" Version="5.1.3" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\TaskListMcp.Core\TaskListMcp.Core.csproj" />
    <ProjectReference Include="..\..\src\TaskListMcp.Data\TaskListMcp.Data.csproj" />
    <ProjectReference Include="..\..\src\TaskListMcp.Models\TaskListMcp.Models.csproj" />
    <ProjectReference Include="..\..\src\TaskListMcp.Server\TaskListMcp.Server.csproj" />
  </ItemGroup>

</Project>
```

### 3. Install Dependencies

```powershell
# Install all test dependencies
cd tests/TaskListMcp.Tests.Unit
dotnet restore

cd ../TaskListMcp.Tests.Integration
dotnet restore

cd ../TaskListMcp.Tests.Performance
dotnet restore

cd ../TaskListMcp.Tests.Docker
dotnet restore
```

## Test Configuration

### 1. Test Database Configuration

#### appsettings.test.json
```json
{
  "TaskListMcp": {
    "Database": {
      "ConnectionString": "Data Source=:memory:",
      "EnableForeignKeys": true,
      "EnableWAL": false
    },
    "Server": {
      "Port": 8080,
      "Host": "localhost"
    },
    "Features": {
      "EnableAdvancedSearch": true,
      "EnableTemplates": true,
      "EnableTags": true,
      "EnableAttributes": true,
      "MaxListDepth": 10,
      "MaxTasksPerList": 1000
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "TaskListMcp": "Debug"
    }
  }
}
```

### 2. Test Base Classes

#### TestFixture.cs
```csharp
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskListMcp.Core.Services;
using TaskListMcp.Data;
using TaskListMcp.Models;

namespace TaskListMcp.Tests.Unit.Helpers;

public class TestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }
    public SqliteConnection Connection { get; private set; }
    public DatabaseManager DatabaseManager { get; private set; }

    public TestFixture()
    {
        var services = new ServiceCollection();
        
        // Configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json", optional: false)
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        // Database (in-memory for tests)
        Connection = new SqliteConnection("Data Source=:memory:");
        Connection.Open();
        
        services.AddSingleton<IDbConnectionFactory>(provider => 
            new SqliteConnectionFactory("Data Source=:memory:"));
        
        services.AddScoped<DatabaseManager>();
        
        // Core services
        services.AddScoped<TaskService>();
        services.AddScoped<ListService>();
        services.AddScoped<TemplateService>();
        services.AddScoped<TagService>();
        services.AddScoped<AttributeService>();
        services.AddScoped<SearchService>();

        ServiceProvider = services.BuildServiceProvider();
        
        // Initialize database
        DatabaseManager = ServiceProvider.GetRequiredService<DatabaseManager>();
        DatabaseManager.InitializeDatabaseAsync().Wait();
    }

    public void Dispose()
    {
        Connection?.Dispose();
        ServiceProvider?.Dispose();
    }
}
```

#### TestDataBuilder.cs
```csharp
using TaskListMcp.Core.Services;
using TaskListMcp.Models;

namespace TaskListMcp.Tests.Unit.Helpers;

public class TestDataBuilder
{
    private readonly TaskService _taskService;
    private readonly ListService _listService;
    private readonly TemplateService _templateService;
    private readonly TagService _tagService;

    public TestDataBuilder(
        TaskService taskService,
        ListService listService,
        TemplateService templateService,
        TagService tagService)
    {
        _taskService = taskService;
        _listService = listService;
        _templateService = templateService;
        _tagService = tagService;
    }

    public async Task<TaskItem> CreateTestTaskAsync(
        string title = "Test Task",
        string? description = null,
        int listId = 1,
        Models.TaskStatus status = Models.TaskStatus.Pending)
    {
        return await _taskService.CreateTaskAsync(title, description, listId, status);
    }

    public async Task<TaskList> CreateTestListAsync(
        string name = "Test List",
        string? description = null,
        int? parentListId = null)
    {
        return await _listService.CreateListAsync(name, description, parentListId);
    }

    public async Task<Template> CreateTestTemplateAsync(
        string name = "Test Template",
        string? description = null)
    {
        // Create a list first
        var list = await CreateTestListAsync("Template Source List");
        await CreateTestTaskAsync("Template Task 1", listId: list.Id);
        await CreateTestTaskAsync("Template Task 2", listId: list.Id);

        return await _templateService.CreateTemplateAsync(list.Id, name, description);
    }

    public async Task<Tag> CreateTestTagAsync(
        string name = "test-tag",
        string color = "#FF0000")
    {
        return await _tagService.CreateTagAsync(name, color);
    }

    public async Task CreateTestHierarchyAsync()
    {
        var rootList = await CreateTestListAsync("Root List");
        var childList1 = await CreateTestListAsync("Child List 1", parentListId: rootList.Id);
        var childList2 = await CreateTestListAsync("Child List 2", parentListId: rootList.Id);
        var grandchildList = await CreateTestListAsync("Grandchild List", parentListId: childList1.Id);

        // Add tasks to various lists
        await CreateTestTaskAsync("Root Task", listId: rootList.Id);
        await CreateTestTaskAsync("Child Task 1", listId: childList1.Id);
        await CreateTestTaskAsync("Child Task 2", listId: childList2.Id);
        await CreateTestTaskAsync("Grandchild Task", listId: grandchildList.Id);
    }

    public async Task CreateLargeDatasetAsync(int taskCount = 1000, int listCount = 50)
    {
        // Create lists
        var lists = new List<TaskList>();
        for (int i = 0; i < listCount; i++)
        {
            var list = await CreateTestListAsync($"List {i + 1}");
            lists.Add(list);
        }

        // Create tasks distributed across lists
        for (int i = 0; i < taskCount; i++)
        {
            var listIndex = i % listCount;
            var listId = lists[listIndex].Id;
            await CreateTestTaskAsync($"Task {i + 1}", $"Description for task {i + 1}", listId);
        }
    }
}
```

#### MockFactory.cs
```csharp
using Microsoft.Extensions.Logging;
using Moq;
using TaskListMcp.Data;

namespace TaskListMcp.Tests.Unit.Helpers;

public static class MockFactory
{
    public static Mock<ILogger<T>> CreateLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    public static Mock<IDbConnectionFactory> CreateConnectionFactory()
    {
        var mock = new Mock<IDbConnectionFactory>();
        // Configure mock behavior as needed
        return mock;
    }

    public static Mock<DatabaseManager> CreateDatabaseManager()
    {
        var connectionFactoryMock = CreateConnectionFactory();
        var loggerMock = CreateLogger<DatabaseManager>();
        
        return new Mock<DatabaseManager>(connectionFactoryMock.Object, loggerMock.Object);
    }
}
```

## Test Execution Scripts

### 1. PowerShell Test Runner

#### run-tests.ps1
```powershell
param(
    [string]$Configuration = "Release",
    [string]$TestCategory = "All",
    [switch]$Coverage,
    [switch]$Parallel
)

Write-Host "Task List MCP Test Runner" -ForegroundColor Green
Write-Host "=========================" -ForegroundColor Green
Write-Host ""

$testProjects = @(
    "tests/TaskListMcp.Tests.Unit",
    "tests/TaskListMcp.Tests.Integration",
    "tests/TaskListMcp.Tests.Performance"
)

$testArgs = @(
    "--configuration", $Configuration,
    "--logger", "trx",
    "--logger", "console;verbosity=normal",
    "--results-directory", "test-results"
)

if ($Coverage) {
    $testArgs += @(
        "--collect", "XPlat Code Coverage",
        "--settings", "coverlet.runsettings"
    )
}

if ($Parallel) {
    $testArgs += @("--parallel")
}

# Create results directory
New-Item -ItemType Directory -Path "test-results" -Force | Out-Null

switch ($TestCategory) {
    "Unit" {
        Write-Host "Running Unit Tests..." -ForegroundColor Yellow
        dotnet test $testProjects[0] @testArgs
    }
    "Integration" {
        Write-Host "Running Integration Tests..." -ForegroundColor Yellow
        dotnet test $testProjects[1] @testArgs
    }
    "Performance" {
        Write-Host "Running Performance Tests..." -ForegroundColor Yellow
        dotnet test $testProjects[2] @testArgs
    }
    "All" {
        foreach ($project in $testProjects) {
            Write-Host "Running tests in $project..." -ForegroundColor Yellow
            dotnet test $project @testArgs
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Tests failed in $project" -ForegroundColor Red
                exit $LASTEXITCODE
            }
        }
    }
}

if ($Coverage) {
    Write-Host ""
    Write-Host "Generating Coverage Report..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-reportgenerator-globaltool
    reportgenerator -reports:"test-results/**/coverage.cobertura.xml" -targetdir:"test-results/coverage" -reporttypes:Html
    Write-Host "Coverage report generated in test-results/coverage/" -ForegroundColor Green
}

Write-Host ""
Write-Host "Test execution completed!" -ForegroundColor Green
```

### 2. Bash Test Runner

#### run-tests.sh
```bash
#!/bin/bash

CONFIG=${1:-Release}
CATEGORY=${2:-All}
COVERAGE=${3:-false}

echo "Task List MCP Test Runner"
echo "========================="
echo ""

TEST_PROJECTS=(
    "tests/TaskListMcp.Tests.Unit"
    "tests/TaskListMcp.Tests.Integration"
    "tests/TaskListMcp.Tests.Performance"
)

TEST_ARGS=(
    "--configuration" "$CONFIG"
    "--logger" "trx"
    "--logger" "console;verbosity=normal"
    "--results-directory" "test-results"
)

if [ "$COVERAGE" = "true" ]; then
    TEST_ARGS+=(
        "--collect" "XPlat Code Coverage"
        "--settings" "coverlet.runsettings"
    )
fi

# Create results directory
mkdir -p test-results

case $CATEGORY in
    "Unit")
        echo "Running Unit Tests..."
        dotnet test "${TEST_PROJECTS[0]}" "${TEST_ARGS[@]}"
        ;;
    "Integration")
        echo "Running Integration Tests..."
        dotnet test "${TEST_PROJECTS[1]}" "${TEST_ARGS[@]}"
        ;;
    "Performance")
        echo "Running Performance Tests..."
        dotnet test "${TEST_PROJECTS[2]}" "${TEST_ARGS[@]}"
        ;;
    "All")
        for project in "${TEST_PROJECTS[@]}"; do
            echo "Running tests in $project..."
            dotnet test "$project" "${TEST_ARGS[@]}"
            if [ $? -ne 0 ]; then
                echo "Tests failed in $project"
                exit 1
            fi
        done
        ;;
esac

if [ "$COVERAGE" = "true" ]; then
    echo ""
    echo "Generating Coverage Report..."
    dotnet tool install -g dotnet-reportgenerator-globaltool
    reportgenerator -reports:"test-results/**/coverage.cobertura.xml" -targetdir:"test-results/coverage" -reporttypes:Html
    echo "Coverage report generated in test-results/coverage/"
fi

echo ""
echo "Test execution completed!"
```

## Code Coverage Configuration

### coverlet.runsettings
```xml
<?xml version="1.0" encoding="utf-8" ?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage">
        <Configuration>
          <Format>opencover,cobertura,lcov</Format>
          <Include>[TaskListMcp.Core]*,[TaskListMcp.Data]*,[TaskListMcp.Server]*</Include>
          <Exclude>[TaskListMcp.Tests.*]*,[*]*.Program,[*]*Migrations*</Exclude>
          <ExcludeByAttribute>Obsolete,GeneratedCodeAttribute,CompilerGeneratedAttribute</ExcludeByAttribute>
          <ExcludeByFile>**/Migrations/**</ExcludeByFile>
          <SkipAutoProps>true</SkipAutoProps>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

## CI/CD Integration

### GitHub Actions Workflow

#### .github/workflows/test.yml
```yaml
name: Test Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --configuration Release --no-restore
      
    - name: Run Unit Tests
      run: dotnet test tests/TaskListMcp.Tests.Unit --configuration Release --logger trx --collect:"XPlat Code Coverage" --results-directory test-results
      
    - name: Upload test results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: unit-test-results
        path: test-results/

  integration-tests:
    runs-on: ubuntu-latest
    needs: unit-tests
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --configuration Release --no-restore
      
    - name: Run Integration Tests
      run: dotnet test tests/TaskListMcp.Tests.Integration --configuration Release --logger trx --results-directory test-results
      
    - name: Upload test results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: integration-test-results
        path: test-results/

  docker-tests:
    runs-on: ubuntu-latest
    needs: [unit-tests, integration-tests]
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Build Docker image
      run: docker build -t task-list-mcp:test .
      
    - name: Run Docker tests
      run: docker-compose -f docker-compose.test.yml up --abort-on-container-exit
      
    - name: Cleanup
      run: docker-compose -f docker-compose.test.yml down

  coverage-report:
    runs-on: ubuntu-latest
    needs: [unit-tests, integration-tests]
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Download test results
      uses: actions/download-artifact@v3
      with:
        name: unit-test-results
        path: test-results/
        
    - name: Generate coverage report
      run: |
        dotnet tool install -g dotnet-reportgenerator-globaltool
        reportgenerator -reports:"test-results/**/coverage.cobertura.xml" -targetdir:"coverage" -reporttypes:Html
        
    - name: Upload coverage reports
      uses: actions/upload-artifact@v3
      with:
        name: coverage-report
        path: coverage/
```

## Docker Test Configuration

### docker-compose.test.yml
```yaml
version: '3.8'

services:
  task-list-mcp-test:
    build:
      context: .
      dockerfile: Dockerfile.test
    environment:
      - ASPNETCORE_ENVIRONMENT=Test
      - TaskListMcp__Database__ConnectionString=Data Source=/tmp/test.db
    volumes:
      - ./test-results:/app/test-results
    command: ["dotnet", "test", "--logger", "trx", "--results-directory", "/app/test-results"]
```

### Dockerfile.test
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS test-runner

WORKDIR /app

# Copy solution and project files
COPY *.sln ./
COPY src/ ./src/
COPY tests/ ./tests/

# Restore dependencies
RUN dotnet restore

# Build the solution
RUN dotnet build --configuration Release --no-restore

# Set the entry point for running tests
ENTRYPOINT ["dotnet", "test", "--configuration", "Release", "--logger", "trx", "--results-directory", "/app/test-results"]
```

## Test Data Management

### Test Database Reset Script

#### reset-test-db.sql
```sql
-- Reset test database to clean state
DELETE FROM task_attributes;
DELETE FROM list_attributes;
DELETE FROM task_tags;
DELETE FROM list_tags;
DELETE FROM template_tasks;
DELETE FROM tasks;
DELETE FROM task_lists WHERE id > 1;
DELETE FROM templates;
DELETE FROM tags;
DELETE FROM attribute_definitions;

-- Reset auto-increment counters
UPDATE sqlite_sequence SET seq = 1 WHERE name = 'tasks';
UPDATE sqlite_sequence SET seq = 1 WHERE name = 'task_lists';
UPDATE sqlite_sequence SET seq = 0 WHERE name = 'templates';
UPDATE sqlite_sequence SET seq = 0 WHERE name = 'tags';
UPDATE sqlite_sequence SET seq = 0 WHERE name = 'attribute_definitions';

-- Ensure default list exists
INSERT OR IGNORE INTO task_lists (id, name, description, created_at, updated_at)
VALUES (1, 'Default List', 'Default task list', datetime('now'), datetime('now'));
```

This comprehensive setup guide provides everything needed to establish a robust testing infrastructure for the Task List MCP Server project. The configuration supports unit testing, integration testing, performance testing, and automated CI/CD workflows with proper code coverage reporting.
