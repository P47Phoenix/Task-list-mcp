# Performance Testing Guide

## Overview
This guide provides detailed instructions for conducting performance testing on the Task List MCP Server, including load testing, stress testing, benchmark testing, and performance monitoring.

## Performance Test Objectives

### Primary Goals
- Validate system performance under normal and peak loads
- Identify performance bottlenecks and resource constraints
- Establish performance baselines and benchmarks
- Ensure scalability requirements are met
- Verify system stability under sustained load

### Key Performance Indicators (KPIs)
- **Response Time**: 95th percentile under 500ms for CRUD operations
- **Throughput**: Handle 100 concurrent users
- **Database Performance**: Query execution under 100ms
- **Memory Usage**: Stay below 512MB under normal load
- **CPU Usage**: Stay below 80% under peak load
- **Database Connections**: Efficient connection pooling

## Test Environment Setup

### Hardware Requirements
```
Minimum Test Environment:
- CPU: 4 cores, 2.4GHz
- RAM: 8GB
- Storage: SSD with 50GB free space
- Network: 1Gbps connection

Recommended Production-like Environment:
- CPU: 8 cores, 3.0GHz
- RAM: 16GB
- Storage: NVMe SSD with 100GB free space
- Network: 10Gbps connection
```

### Software Dependencies
```bash
# Install performance testing tools
dotnet tool install -g NBomber.Templates
dotnet tool install -g BenchmarkDotNet.Tool
dotnet tool install -g dotnet-counters
dotnet tool install -g dotnet-trace

# Install monitoring tools
npm install -g clinic
pip install locust
```

## Performance Test Framework

### BenchmarkDotNet Configuration

#### BenchmarkConfig.cs
```csharp
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default
            .WithRuntime(CoreRuntime.Core80)
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithWarmupCount(3)
            .WithIterationCount(10));
            
        AddExporter(BenchmarkDotNet.Exporters.MarkdownExporter.GitHub);
        AddExporter(BenchmarkDotNet.Exporters.HtmlExporter.Default);
        AddLogger(BenchmarkDotNet.Loggers.ConsoleLogger.Default);
    }
}
```

### NBomber Load Testing Configuration

#### LoadTestConfig.cs
```csharp
using NBomber.CSharp;
using NBomber.Contracts;

public class LoadTestConfig
{
    public static NBomberContext CreateScenario(string scenarioName, Func<IScenarioContext, Task<Response>> action)
    {
        var scenario = Scenario.Create(scenarioName, action)
            .WithLoadSimulations(
                Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromMinutes(2)),
                Simulation.RampingInjectPerSec(rate: 20, during: TimeSpan.FromMinutes(3)),
                Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(5))
            );

        return NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("performance-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv);
    }
}
```

## Benchmark Tests

### Core Service Benchmarks

#### TaskServiceBenchmarks.cs
```csharp
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class TaskServiceBenchmarks
{
    private TaskService _taskService;
    private ServiceProvider _serviceProvider;
    private List<int> _taskIds;

    [GlobalSetup]
    public async Task Setup()
    {
        // Setup test environment
        var services = new ServiceCollection();
        // Configure services...
        _serviceProvider = services.BuildServiceProvider();
        _taskService = _serviceProvider.GetRequiredService<TaskService>();
        
        // Pre-create test data
        _taskIds = new List<int>();
        for (int i = 0; i < 1000; i++)
        {
            var task = await _taskService.CreateTaskAsync($"Benchmark Task {i}");
            _taskIds.Add(task.Id);
        }
    }

    [Benchmark]
    public async Task CreateTask()
    {
        await _taskService.CreateTaskAsync("Benchmark Task", "Description");
    }

    [Benchmark]
    public async Task GetTask()
    {
        var randomId = _taskIds[Random.Shared.Next(_taskIds.Count)];
        await _taskService.GetTaskByIdAsync(randomId);
    }

    [Benchmark]
    public async Task UpdateTask()
    {
        var randomId = _taskIds[Random.Shared.Next(_taskIds.Count)];
        await _taskService.UpdateTaskAsync(randomId, title: "Updated Title");
    }

    [Benchmark]
    public async Task ListTasks()
    {
        await _taskService.GetTasksAsync(limit: 50);
    }

    [Benchmark]
    public async Task SearchTasks()
    {
        await _taskService.SearchTasksAsync("Benchmark");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }
}
```

#### DatabaseBenchmarks.cs
```csharp
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class DatabaseBenchmarks
{
    private DatabaseManager _databaseManager;
    private ServiceProvider _serviceProvider;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        // Configure services...
        _serviceProvider = services.BuildServiceProvider();
        _databaseManager = _serviceProvider.GetRequiredService<DatabaseManager>();
    }

    [Benchmark]
    public async Task BulkInsert()
    {
        using var connection = await _databaseManager.GetConnectionAsync();
        using var transaction = connection.BeginTransaction();
        
        for (int i = 0; i < 100; i++)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO tasks (title, status, created_at, updated_at) VALUES (@title, @status, @created, @updated)";
            command.Parameters.AddWithValue("@title", $"Bulk Task {i}");
            command.Parameters.AddWithValue("@status", "Pending");
            command.Parameters.AddWithValue("@created", DateTime.UtcNow);
            command.Parameters.AddWithValue("@updated", DateTime.UtcNow);
            await command.ExecuteNonQueryAsync();
        }
        
        await transaction.CommitAsync();
    }

    [Benchmark]
    public async Task ComplexQuery()
    {
        using var connection = await _databaseManager.GetConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT t.*, l.name as list_name, COUNT(tt.tag_id) as tag_count
            FROM tasks t
            LEFT JOIN task_lists l ON t.list_id = l.id
            LEFT JOIN task_tags tt ON t.id = tt.task_id
            WHERE t.status = 'Pending'
            GROUP BY t.id
            ORDER BY t.created_at DESC
            LIMIT 50";
        
        using var reader = await command.ExecuteReaderAsync();
        var results = new List<object>();
        while (await reader.ReadAsync())
        {
            results.Add(new { 
                Id = reader.GetInt32("id"),
                Title = reader.GetString("title"),
                ListName = reader.IsDBNull("list_name") ? null : reader.GetString("list_name"),
                TagCount = reader.GetInt32("tag_count")
            });
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }
}
```

## Load Testing

### NBomber Load Tests

#### TaskLoadTests.cs
```csharp
using NBomber.CSharp;
using NBomber.Contracts;
using TaskListMcp.Core.Services;

public class TaskLoadTests
{
    public static void RunTaskCreationLoadTest()
    {
        var scenario = Scenario.Create("task_creation_load", async context =>
        {
            var taskService = context.GlobalCustomSettings.Get<TaskService>();
            
            try
            {
                var task = await taskService.CreateTaskAsync(
                    $"Load Test Task {context.ScenarioInfo.CurrentOperation}",
                    $"Created during load test iteration {context.ScenarioInfo.CurrentIteration}"
                );
                
                return Response.Ok(task.Id.ToString());
            }
            catch (Exception ex)
            {
                return Response.Fail(ex.Message);
            }
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 5, during: TimeSpan.FromMinutes(1)),
            Simulation.RampingInjectPerSec(rate: 20, during: TimeSpan.FromMinutes(2)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(3)),
            Simulation.RampingInjectPerSec(rate: 100, during: TimeSpan.FromMinutes(1))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("load-test-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();
    }

    public static void RunMixedWorkloadTest()
    {
        var createScenario = Scenario.Create("create_tasks", async context =>
        {
            var taskService = context.GlobalCustomSettings.Get<TaskService>();
            var task = await taskService.CreateTaskAsync($"Task {context.ScenarioInfo.CurrentOperation}");
            return Response.Ok();
        })
        .WithWeight(30)
        .WithLoadSimulations(Simulation.KeepConstant(copies: 15, during: TimeSpan.FromMinutes(5)));

        var readScenario = Scenario.Create("read_tasks", async context =>
        {
            var taskService = context.GlobalCustomSettings.Get<TaskService>();
            var tasks = await taskService.GetTasksAsync(limit: 20);
            return Response.Ok();
        })
        .WithWeight(50)
        .WithLoadSimulations(Simulation.KeepConstant(copies: 25, during: TimeSpan.FromMinutes(5)));

        var updateScenario = Scenario.Create("update_tasks", async context =>
        {
            var taskService = context.GlobalCustomSettings.Get<TaskService>();
            // Get random existing task and update it
            var tasks = await taskService.GetTasksAsync(limit: 100);
            if (tasks.Any())
            {
                var randomTask = tasks[Random.Shared.Next(tasks.Count)];
                await taskService.UpdateTaskAsync(randomTask.Id, title: $"Updated {DateTime.Now:HH:mm:ss}");
            }
            return Response.Ok();
        })
        .WithWeight(20)
        .WithLoadSimulations(Simulation.KeepConstant(copies: 10, during: TimeSpan.FromMinutes(5)));

        NBomberRunner
            .RegisterScenarios(createScenario, readScenario, updateScenario)
            .WithReportFolder("mixed-workload-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();
    }
}
```

#### MCPToolLoadTests.cs
```csharp
public class MCPToolLoadTests
{
    public static void RunMCPToolsLoadTest()
    {
        var createTaskScenario = Scenario.Create("mcp_create_task", async context =>
        {
            var mcpRequest = new
            {
                method = "tools/call",
                @params = new
                {
                    name = "create_task",
                    arguments = new
                    {
                        title = $"Load Test Task {context.ScenarioInfo.CurrentOperation}",
                        description = "Created via MCP during load test",
                        listId = 1
                    }
                }
            };

            // Simulate MCP tool call
            var httpClient = context.GlobalCustomSettings.Get<HttpClient>();
            var json = JsonSerializer.Serialize(mcpRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync("/mcp/tools", content);
            
            if (response.IsSuccessStatusCode)
                return Response.Ok();
            else
                return Response.Fail($"HTTP {response.StatusCode}");
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromMinutes(2)),
            Simulation.KeepConstant(copies: 30, during: TimeSpan.FromMinutes(3))
        );

        var listTasksScenario = Scenario.Create("mcp_list_tasks", async context =>
        {
            var mcpRequest = new
            {
                method = "tools/call",
                @params = new
                {
                    name = "list_tasks",
                    arguments = new
                    {
                        limit = 20,
                        status = "Pending"
                    }
                }
            };

            var httpClient = context.GlobalCustomSettings.Get<HttpClient>();
            var json = JsonSerializer.Serialize(mcpRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync("/mcp/tools", content);
            
            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 20, during: TimeSpan.FromMinutes(5))
        );

        NBomberRunner
            .RegisterScenarios(createTaskScenario, listTasksScenario)
            .WithReportFolder("mcp-tools-load-reports")
            .Run();
    }
}
```

## Stress Testing

### Stress Test Scenarios

#### StressTests.cs
```csharp
public class StressTests
{
    public static void RunDatabaseStressTest()
    {
        var scenario = Scenario.Create("database_stress", async context =>
        {
            var services = context.GlobalCustomSettings.Get<IServiceProvider>();
            var taskService = services.GetRequiredService<TaskService>();
            var listService = services.GetRequiredService<ListService>();
            
            // Simulate complex operations
            var tasks = new List<Task>();
            
            // Create multiple tasks concurrently
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(taskService.CreateTaskAsync($"Stress Task {i}"));
            }
            
            // Create lists concurrently
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(listService.CreateListAsync($"Stress List {i}"));
            }
            
            await Task.WhenAll(tasks);
            return Response.Ok();
        })
        .WithLoadSimulations(
            Simulation.RampingInjectPerSec(rate: 50, during: TimeSpan.FromMinutes(2)),
            Simulation.KeepConstant(copies: 100, during: TimeSpan.FromMinutes(10)),
            Simulation.RampingInjectPerSec(rate: 200, during: TimeSpan.FromMinutes(2))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("stress-test-reports")
            .Run();
    }

    public static void RunMemoryStressTest()
    {
        var scenario = Scenario.Create("memory_stress", async context =>
        {
            var services = context.GlobalCustomSettings.Get<IServiceProvider>();
            var searchService = services.GetRequiredService<SearchService>();
            
            // Perform memory-intensive operations
            var largeResults = await searchService.SearchTasksAsync("*"); // Get all tasks
            var processed = largeResults.Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                ProcessedAt = DateTime.UtcNow,
                Hash = t.Title?.GetHashCode()
            }).ToList();
            
            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            return Response.Ok($"Processed {processed.Count} items");
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 20, during: TimeSpan.FromMinutes(5))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("memory-stress-reports")
            .Run();
    }
}
```

## Performance Monitoring

### Real-time Monitoring Setup

#### PerformanceMonitor.cs
```csharp
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class PerformanceMonitor : BackgroundService
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _memoryCounter;

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger;
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var cpuUsage = _cpuCounter.NextValue();
            var availableMemory = _memoryCounter.NextValue();
            var workingSet = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024; // MB

            _logger.LogInformation("Performance Metrics - CPU: {CpuUsage:F1}%, Memory: {AvailableMemory:F0}MB available, Working Set: {WorkingSet:F0}MB",
                cpuUsage, availableMemory, workingSet);

            // Alert on high resource usage
            if (cpuUsage > 80)
            {
                _logger.LogWarning("High CPU usage detected: {CpuUsage:F1}%", cpuUsage);
            }

            if (workingSet > 512)
            {
                _logger.LogWarning("High memory usage detected: {WorkingSet:F0}MB", workingSet);
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    public override void Dispose()
    {
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();
        base.Dispose();
    }
}
```

### Database Performance Monitoring

#### DatabaseMetrics.cs
```csharp
public class DatabaseMetrics
{
    private readonly DatabaseManager _databaseManager;
    private readonly ILogger<DatabaseMetrics> _logger;

    public DatabaseMetrics(DatabaseManager databaseManager, ILogger<DatabaseMetrics> logger)
    {
        _databaseManager = databaseManager;
        _logger = logger;
    }

    public async Task<DatabasePerformanceReport> GenerateReportAsync()
    {
        var report = new DatabasePerformanceReport();

        using var connection = await _databaseManager.GetConnectionAsync();

        // Query execution statistics
        var queries = new[]
        {
            ("SELECT COUNT(*) FROM tasks", "Task Count"),
            ("SELECT COUNT(*) FROM task_lists", "List Count"),
            ("SELECT COUNT(*) FROM tags", "Tag Count"),
            ("SELECT AVG(CAST(strftime('%s', updated_at) - strftime('%s', created_at) AS REAL)) FROM tasks WHERE status = 'Completed'", "Avg Task Duration")
        };

        foreach (var (query, description) in queries)
        {
            var stopwatch = Stopwatch.StartNew();
            using var command = connection.CreateCommand();
            command.CommandText = query;
            
            var result = await command.ExecuteScalarAsync();
            stopwatch.Stop();

            report.QueryMetrics.Add(new QueryMetric
            {
                Description = description,
                ExecutionTime = stopwatch.Elapsed,
                Result = result?.ToString()
            });
        }

        // Database size information
        using var sizeCommand = connection.CreateCommand();
        sizeCommand.CommandText = "SELECT page_count * page_size as size FROM pragma_page_count(), pragma_page_size()";
        var dbSize = Convert.ToInt64(await sizeCommand.ExecuteScalarAsync());
        report.DatabaseSizeBytes = dbSize;

        return report;
    }
}

public class DatabasePerformanceReport
{
    public List<QueryMetric> QueryMetrics { get; set; } = new();
    public long DatabaseSizeBytes { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class QueryMetric
{
    public string Description { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public string? Result { get; set; }
}
```

## Performance Test Execution

### Test Execution Scripts

#### run-performance-tests.ps1
```powershell
param(
    [string]$TestType = "All",
    [int]$Duration = 5,
    [int]$Users = 50
)

Write-Host "Performance Test Runner" -ForegroundColor Green
Write-Host "======================" -ForegroundColor Green

# Create reports directory
New-Item -ItemType Directory -Path "performance-reports" -Force | Out-Null

switch ($TestType) {
    "Benchmark" {
        Write-Host "Running Benchmark Tests..." -ForegroundColor Yellow
        dotnet run --project tests/TaskListMcp.Tests.Performance --configuration Release -- --benchmarks
    }
    "Load" {
        Write-Host "Running Load Tests..." -ForegroundColor Yellow
        dotnet run --project tests/TaskListMcp.Tests.Performance --configuration Release -- --load --duration $Duration --users $Users
    }
    "Stress" {
        Write-Host "Running Stress Tests..." -ForegroundColor Yellow
        dotnet run --project tests/TaskListMcp.Tests.Performance --configuration Release -- --stress --duration $Duration
    }
    "All" {
        Write-Host "Running All Performance Tests..." -ForegroundColor Yellow
        
        # Benchmarks
        dotnet run --project tests/TaskListMcp.Tests.Performance --configuration Release -- --benchmarks
        
        # Load Tests
        dotnet run --project tests/TaskListMcp.Tests.Performance --configuration Release -- --load --duration $Duration --users $Users
        
        # Stress Tests
        dotnet run --project tests/TaskListMcp.Tests.Performance --configuration Release -- --stress --duration $Duration
    }
}

Write-Host ""
Write-Host "Performance tests completed! Check performance-reports directory for results." -ForegroundColor Green
```

#### docker-performance-test.yml
```yaml
version: '3.8'

services:
  task-list-mcp-perf:
    build:
      context: .
      dockerfile: Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - TaskListMcp__Database__ConnectionString=Data Source=/app/data/perf-test.db
    volumes:
      - ./performance-data:/app/data
      - ./performance-reports:/app/reports
    ports:
      - "8080:8080"
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 1G
        reservations:
          cpus: '1.0'
          memory: 512M

  performance-tester:
    image: peterevans/locust:latest
    volumes:
      - ./performance-tests:/mnt/locust
    command: -f /mnt/locust/locustfile.py --host=http://task-list-mcp-perf:8080 --users=100 --spawn-rate=10 --run-time=5m --html=/mnt/locust/report.html
    depends_on:
      - task-list-mcp-perf
```

## Performance Analysis

### Report Generation

#### PerformanceReportGenerator.cs
```csharp
public class PerformanceReportGenerator
{
    public static void GenerateHtmlReport(string reportPath, PerformanceTestResults results)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Task List MCP Performance Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .metric {{ margin: 10px 0; padding: 10px; border: 1px solid #ddd; }}
        .pass {{ background-color: #d4edda; }}
        .fail {{ background-color: #f8d7da; }}
        table {{ border-collapse: collapse; width: 100%; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
    </style>
</head>
<body>
    <h1>Task List MCP Performance Test Report</h1>
    <p>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
    
    <h2>Summary</h2>
    <div class=""metric {(results.OverallPass ? "pass" : "fail")}"">
        <strong>Overall Result: {(results.OverallPass ? "PASS" : "FAIL")}</strong>
    </div>
    
    <h2>Benchmark Results</h2>
    <table>
        <tr>
            <th>Operation</th>
            <th>Mean (ms)</th>
            <th>95th Percentile (ms)</th>
            <th>Throughput (ops/sec)</th>
            <th>Status</th>
        </tr>
        {string.Join("", results.BenchmarkResults.Select(r => $@"
        <tr>
            <td>{r.Operation}</td>
            <td>{r.MeanTime:F2}</td>
            <td>{r.Percentile95:F2}</td>
            <td>{r.Throughput:F2}</td>
            <td class=""{(r.Pass ? "pass" : "fail")}"">{(r.Pass ? "PASS" : "FAIL")}</td>
        </tr>"))}
    </table>
    
    <h2>Load Test Results</h2>
    <div class=""metric"">
        <strong>Peak Users:</strong> {results.LoadTestResults.PeakUsers}<br>
        <strong>Total Requests:</strong> {results.LoadTestResults.TotalRequests}<br>
        <strong>Error Rate:</strong> {results.LoadTestResults.ErrorRate:P2}<br>
        <strong>Average Response Time:</strong> {results.LoadTestResults.AverageResponseTime:F2}ms
    </div>
    
    <h2>Resource Usage</h2>
    <div class=""metric"">
        <strong>Peak CPU:</strong> {results.ResourceUsage.PeakCpuUsage:P1}<br>
        <strong>Peak Memory:</strong> {results.ResourceUsage.PeakMemoryUsage:F0}MB<br>
        <strong>Database Size:</strong> {results.ResourceUsage.DatabaseSize / 1024 / 1024:F2}MB
    </div>
</body>
</html>";

        File.WriteAllText(reportPath, html);
    }
}
```

This performance testing guide provides comprehensive coverage for evaluating the Task List MCP Server's performance characteristics, identifying bottlenecks, and ensuring the system meets its performance requirements under various load conditions.
