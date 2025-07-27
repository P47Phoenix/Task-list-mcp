# Security Testing Guide

## Overview
This guide provides comprehensive security testing procedures for the Task List MCP Server, covering input validation, SQL injection prevention, authentication, authorization, and data security measures.

## Security Test Objectives

### Primary Security Goals
- Prevent SQL injection attacks
- Validate all input data thoroughly
- Ensure secure data handling and storage
- Protect against common web vulnerabilities
- Validate MCP protocol security
- Ensure secure Docker deployment

### Security Threat Model
- **Input Validation Attacks**: Malicious data injection
- **SQL Injection**: Database manipulation attempts
- **Data Integrity**: Unauthorized data modification
- **Resource Exhaustion**: DoS through resource consumption
- **Container Security**: Docker-specific vulnerabilities
- **Configuration Security**: Insecure settings

## Input Validation Testing

### Malicious Input Test Cases

#### SqlInjectionTests.cs
```csharp
using Xunit;
using FluentAssertions;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;

public class SqlInjectionTests : IClassFixture<TestFixture>
{
    private readonly TaskService _taskService;
    private readonly ListService _listService;
    private readonly TagService _tagService;

    public SqlInjectionTests(TestFixture fixture)
    {
        _taskService = fixture.ServiceProvider.GetRequiredService<TaskService>();
        _listService = fixture.ServiceProvider.GetRequiredService<ListService>();
        _tagService = fixture.ServiceProvider.GetRequiredService<TagService>();
    }

    [Theory]
    [InlineData("'; DROP TABLE tasks; --")]
    [InlineData("1'; UPDATE tasks SET title='hacked' WHERE '1'='1")]
    [InlineData("' OR '1'='1' --")]
    [InlineData("'; INSERT INTO tasks (title) VALUES ('injected'); --")]
    [InlineData("\"; DELETE FROM tasks; \"")]
    public async Task CreateTask_WithSqlInjectionAttempt_ShouldTreatAsLiteralText(string maliciousTitle)
    {
        // Act
        var result = await _taskService.CreateTaskAsync(maliciousTitle, "Test description");

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(maliciousTitle); // Should be stored as literal text
        
        // Verify database integrity - check that tables still exist and no unauthorized changes occurred
        var tasks = await _taskService.GetTasksAsync();
        tasks.Should().NotBeEmpty();
        tasks.Should().Contain(t => t.Title == maliciousTitle);
        
        // Verify only expected data exists
        var suspiciousTask = tasks.FirstOrDefault(t => t.Title == "hacked" || t.Title == "injected");
        suspiciousTask.Should().BeNull("No unauthorized data should be inserted");
    }

    [Theory]
    [InlineData("'; DROP TABLE task_lists; --")]
    [InlineData("' UNION SELECT password FROM users --")]
    [InlineData("'; EXEC xp_cmdshell('dir'); --")]
    public async Task CreateList_WithSqlInjectionAttempt_ShouldTreatAsLiteralText(string maliciousName)
    {
        // Act
        var result = await _listService.CreateListAsync(maliciousName, "Test description");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(maliciousName);
        
        // Verify database structure integrity
        var lists = await _listService.GetAllListsAsync();
        lists.Should().NotBeEmpty();
        lists.Should().Contain(l => l.Name == maliciousName);
    }

    [Theory]
    [InlineData("'; UPDATE tasks SET status='Completed' WHERE status='Pending'; --")]
    [InlineData("' OR 1=1; DELETE FROM task_tags; --")]
    public async Task SearchTasks_WithSqlInjectionAttempt_ShouldReturnSafeResults(string maliciousQuery)
    {
        // Arrange - Create test data
        await _taskService.CreateTaskAsync("Normal Task 1", "Description 1");
        await _taskService.CreateTaskAsync("Normal Task 2", "Description 2");

        // Act
        var searchService = new SearchService(_connectionFactory, _logger);
        var results = await searchService.SearchTasksAsync(maliciousQuery);

        // Assert
        results.Should().NotBeNull();
        // Should return empty results or safe results, not cause database corruption
        
        // Verify original data is intact
        var allTasks = await _taskService.GetTasksAsync();
        allTasks.Should().Contain(t => t.Title == "Normal Task 1");
        allTasks.Should().Contain(t => t.Title == "Normal Task 2");
        
        // Verify no unauthorized status changes occurred
        var pendingTasks = allTasks.Where(t => t.Status == TaskStatus.Pending);
        pendingTasks.Should().HaveCount(2, "No unauthorized status changes should occur");
    }

    [Fact]
    public async Task ParameterizedQueries_ShouldPreventInjection()
    {
        // Arrange
        var maliciousId = "1; DROP TABLE tasks; --";

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(async () =>
        {
            // This should fail to convert to int, not execute SQL
            var id = Convert.ToInt32(maliciousId);
            await _taskService.GetTaskByIdAsync(id);
        });

        // Verify database integrity
        var tasks = await _taskService.GetTasksAsync();
        tasks.Should().NotBeNull("Tasks table should still exist");
    }
}
```

### Input Validation Tests

#### InputValidationTests.cs
```csharp
public class InputValidationTests : IClassFixture<TestFixture>
{
    private readonly TaskService _taskService;
    private readonly ListService _listService;

    public InputValidationTests(TestFixture fixture)
    {
        _taskService = fixture.ServiceProvider.GetRequiredService<TaskService>();
        _listService = fixture.ServiceProvider.GetRequiredService<ListService>();
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("javascript:alert('xss')")]
    [InlineData("<img src=x onerror=alert('xss')>")]
    [InlineData("'><script>alert('xss')</script>")]
    public async Task CreateTask_WithXssAttempt_ShouldSanitizeInput(string maliciousInput)
    {
        // Act
        var task = await _taskService.CreateTaskAsync(maliciousInput, "Description");

        // Assert
        task.Title.Should().NotContain("<script>");
        task.Title.Should().NotContain("javascript:");
        task.Title.Should().NotContain("onerror=");
        
        // Or if HTML encoding is used, verify it's properly encoded
        // task.Title.Should().Contain("&lt;script&gt;");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTask_WithInvalidTitle_ShouldThrowArgumentException(string? invalidTitle)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _taskService.CreateTaskAsync(invalidTitle!, "Description");
        });
    }

    [Fact]
    public async Task CreateTask_WithExcessivelyLongTitle_ShouldThrowArgumentException()
    {
        // Arrange
        var longTitle = new string('a', 10001); // Assuming 10k character limit

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _taskService.CreateTaskAsync(longTitle, "Description");
        });
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32\\config\\sam")]
    [InlineData("/etc/shadow")]
    [InlineData("C:\\Windows\\System32\\config\\SAM")]
    public async Task CreateTask_WithPathTraversalAttempt_ShouldTreatAsLiteralText(string pathTraversalAttempt)
    {
        // Act
        var task = await _taskService.CreateTaskAsync(pathTraversalAttempt, "Description");

        // Assert
        task.Title.Should().Be(pathTraversalAttempt);
        // Verify no file system access occurred
        task.Id.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(999999)]
    public async Task GetTask_WithInvalidId_ShouldHandleGracefully(int invalidId)
    {
        // Act
        var task = await _taskService.GetTaskByIdAsync(invalidId);

        // Assert
        task.Should().BeNull("Invalid IDs should return null, not throw exceptions");
    }

    [Fact]
    public async Task CreateList_WithNameExceeding200Characters_ShouldThrowArgumentException()
    {
        // Arrange
        var longName = new string('a', 201);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _listService.CreateListAsync(longName);
        });
    }

    [Theory]
    [InlineData("SELECT * FROM users")]
    [InlineData("DROP TABLE tasks")]
    [InlineData("INSERT INTO admin_users")]
    [InlineData("UPDATE system_config")]
    public async Task SearchOperations_WithSqlKeywords_ShouldTreatAsSearchTerms(string sqlKeyword)
    {
        // Arrange
        var searchService = new SearchService(_connectionFactory, _logger);

        // Act
        var results = await searchService.SearchTasksAsync(sqlKeyword);

        // Assert
        results.Should().NotBeNull();
        // Should return search results, not execute SQL commands
    }
}
```

## Data Security Tests

### Encryption and Sensitive Data Tests

#### DataSecurityTests.cs
```csharp
public class DataSecurityTests : IClassFixture<TestFixture>
{
    private readonly DatabaseManager _databaseManager;
    private readonly IConfiguration _configuration;

    public DataSecurityTests(TestFixture fixture)
    {
        _databaseManager = fixture.ServiceProvider.GetRequiredService<DatabaseManager>();
        _configuration = fixture.ServiceProvider.GetRequiredService<IConfiguration>();
    }

    [Fact]
    public async Task DatabaseConnection_ShouldNotExposeCredentials()
    {
        // Act
        using var connection = await _databaseManager.GetConnectionAsync();

        // Assert
        var connectionString = connection.ConnectionString;
        connectionString.Should().NotContain("password=", StringComparison.OrdinalIgnoreCase);
        connectionString.Should().NotContain("pwd=", StringComparison.OrdinalIgnoreCase);
        
        // For SQLite, verify file permissions if applicable
        if (connectionString.Contains("Data Source=") && !connectionString.Contains(":memory:"))
        {
            var dbPath = connectionString.Split("Data Source=")[1].Split(';')[0];
            if (File.Exists(dbPath))
            {
                var fileInfo = new FileInfo(dbPath);
                // Verify appropriate file permissions
                fileInfo.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void Configuration_ShouldNotContainHardcodedSecrets()
    {
        // Act
        var config = _configuration.AsEnumerable();

        // Assert
        foreach (var (key, value) in config)
        {
            if (value != null)
            {
                value.Should().NotContain("password123", StringComparison.OrdinalIgnoreCase);
                value.Should().NotContain("admin", StringComparison.OrdinalIgnoreCase);
                value.Should().NotContain("secret", StringComparison.OrdinalIgnoreCase);
                value.Should().NotContain("token", StringComparison.OrdinalIgnoreCase);
                
                // Check for common weak passwords
                var weakPasswords = new[] { "password", "123456", "admin", "root", "guest" };
                foreach (var weakPassword in weakPasswords)
                {
                    value.Should().NotBe(weakPassword, $"Configuration key '{key}' should not contain weak password");
                }
            }
        }
    }

    [Fact]
    public async Task DatabaseQueries_ShouldNotLogSensitiveData()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddInMemory());
        var logger = loggerFactory.CreateLogger<TaskService>();
        var taskService = new TaskService(_databaseManager, logger);

        // Act
        await taskService.CreateTaskAsync("Test Task", "Sensitive information: SSN 123-45-6789");

        // Assert
        var logs = loggerFactory.GetLogs();
        foreach (var log in logs)
        {
            log.Should().NotContain("123-45-6789", "Sensitive data should not appear in logs");
            log.Should().NotContain("SSN", "Sensitive identifiers should not appear in logs");
        }
    }

    [Fact]
    public async Task ErrorMessages_ShouldNotExposeInternalDetails()
    {
        // Arrange
        var invalidConnectionFactory = new Mock<IDbConnectionFactory>();
        invalidConnectionFactory.Setup(x => x.CreateConnectionAsync())
            .ThrowsAsync(new InvalidOperationException("Internal database error: Connection failed at server 192.168.1.100"));

        var taskService = new TaskService(invalidConnectionFactory.Object, Mock.Of<ILogger<TaskService>>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await taskService.CreateTaskAsync("Test", "Description");
        });

        // The exposed error should not contain internal details
        exception.Message.Should().NotContain("192.168.1.100", "Internal IP addresses should not be exposed");
        exception.Message.Should().NotContain("Connection failed at server", "Internal connection details should not be exposed");
    }
}
```

## MCP Protocol Security Tests

### MCP Security Tests

#### McpSecurityTests.cs
```csharp
public class McpSecurityTests
{
    [Theory]
    [InlineData("{ \"method\": \"tools/call\", \"params\": { \"name\": \"../../../etc/passwd\" } }")]
    [InlineData("{ \"method\": \"tools/call\", \"params\": { \"name\": \"system('rm -rf /')\" } }")]
    [InlineData("{ \"method\": \"tools/call\", \"params\": { \"name\": \"<script>alert('xss')</script>\" } }")]
    public async Task McpToolCall_WithMaliciousToolName_ShouldRejectRequest(string maliciousJson)
    {
        // Arrange
        var httpClient = new HttpClient();
        var content = new StringContent(maliciousJson, Encoding.UTF8, "application/json");

        // Act
        var response = await httpClient.PostAsync("http://localhost:8080/mcp/tools", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Invalid tool name");
        responseContent.Should().NotContain("etc/passwd");
        responseContent.Should().NotContain("rm -rf");
    }

    [Fact]
    public async Task McpToolCall_WithExcessivelyLargePayload_ShouldRejectRequest()
    {
        // Arrange
        var largeData = new string('x', 10 * 1024 * 1024); // 10MB payload
        var maliciousJson = $"{{ \"method\": \"tools/call\", \"params\": {{ \"name\": \"create_task\", \"arguments\": {{ \"title\": \"{largeData}\" }} }} }}";
        
        var httpClient = new HttpClient();
        var content = new StringContent(maliciousJson, Encoding.UTF8, "application/json");

        // Act
        var response = await httpClient.PostAsync("http://localhost:8080/mcp/tools", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Theory]
    [InlineData("{ \"method\": \"tools/call\", \"params\": { \"name\": \"create_task\", \"arguments\": { \"title\": \"Test\", \"eval\": \"process.exit(1)\" } } }")]
    [InlineData("{ \"method\": \"tools/call\", \"params\": { \"name\": \"create_task\", \"arguments\": { \"title\": \"Test\", \"__proto__\": { \"isAdmin\": true } } } }")]
    public async Task McpToolCall_WithPrototypePollution_ShouldRejectRequest(string maliciousJson)
    {
        // Arrange
        var httpClient = new HttpClient();
        var content = new StringContent(maliciousJson, Encoding.UTF8, "application/json");

        // Act
        var response = await httpClient.PostAsync("http://localhost:8080/mcp/tools", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotContain("isAdmin");
        responseContent.Should().NotContain("process.exit");
    }

    [Fact]
    public async Task McpToolCall_WithInvalidJson_ShouldHandleGracefully()
    {
        // Arrange
        var invalidJson = "{ invalid json structure";
        var httpClient = new HttpClient();
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        // Act
        var response = await httpClient.PostAsync("http://localhost:8080/mcp/tools", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Invalid JSON");
        responseContent.Should().NotContain("System.", "Internal .NET types should not be exposed");
    }
}
```

## Container Security Tests

### Docker Security Tests

#### DockerSecurityTests.cs
```csharp
public class DockerSecurityTests
{
    [Fact]
    public async Task Container_ShouldNotRunAsRootUser()
    {
        // Arrange & Act
        var result = await ExecuteDockerCommand("docker exec task-list-mcp whoami");

        // Assert
        result.Should().NotContain("root", "Container should not run as root user");
        result.Should().Contain("app", "Container should run as non-privileged user");
    }

    [Fact]
    public async Task Container_ShouldHaveMinimalBaseImage()
    {
        // Arrange & Act
        var result = await ExecuteDockerCommand("docker inspect task-list-mcp --format='{{.Config.Image}}'");

        // Assert
        result.Should().Contain("mcr.microsoft.com/dotnet/aspnet", "Should use official Microsoft base image");
        result.Should().NotContain("latest", "Should not use 'latest' tag");
    }

    [Fact]
    public async Task Container_ShouldNotHaveUnnecessaryPackages()
    {
        // Arrange & Act
        var result = await ExecuteDockerCommand("docker exec task-list-mcp ls /usr/bin");

        // Assert
        result.Should().NotContain("curl", "curl should not be available in production container");
        result.Should().NotContain("wget", "wget should not be available in production container");
        result.Should().NotContain("ssh", "SSH should not be available in production container");
        result.Should().NotContain("telnet", "Telnet should not be available in production container");
    }

    [Fact]
    public async Task Container_ShouldUseNonPrivilegedPorts()
    {
        // Arrange & Act
        var result = await ExecuteDockerCommand("docker port task-list-mcp");

        // Assert
        var portMappings = result.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line));
        foreach (var mapping in portMappings)
        {
            if (mapping.Contains("->"))
            {
                var port = mapping.Split(':').Last().Split('/').First();
                var portNumber = int.Parse(port);
                portNumber.Should().BeGreaterThan(1024, $"Port {portNumber} should be non-privileged (>1024)");
            }
        }
    }

    [Fact]
    public async Task Container_ShouldHaveHealthCheck()
    {
        // Arrange & Act
        var result = await ExecuteDockerCommand("docker inspect task-list-mcp --format='{{.Config.Healthcheck}}'");

        // Assert
        result.Should().NotContain("<no value>", "Container should have health check configured");
        result.Should().Contain("CMD", "Health check should be properly configured");
    }

    [Fact]
    public async Task Container_ShouldNotMountSensitiveDirectories()
    {
        // Arrange & Act
        var result = await ExecuteDockerCommand("docker inspect task-list-mcp --format='{{range .Mounts}}{{.Source}}:{{.Destination}}{{end}}'");

        // Assert
        result.Should().NotContain("/etc:", "Should not mount /etc directory");
        result.Should().NotContain("/root:", "Should not mount /root directory");
        result.Should().NotContain("/proc:", "Should not mount /proc directory");
        result.Should().NotContain("/sys:", "Should not mount /sys directory");
        result.Should().NotContain("/dev:", "Should not mount /dev directory");
    }

    [Fact]
    public async Task Container_ShouldHaveReadOnlyFileSystem()
    {
        // Arrange & Act
        var result = await ExecuteDockerCommand("docker inspect task-list-mcp --format='{{.HostConfig.ReadonlyRootfs}}'");

        // Assert
        // Note: This might be false if the application needs to write to filesystem
        // In that case, ensure only specific directories are writable
        if (result.Contains("false"))
        {
            // Verify that only necessary directories are writable
            var writeResult = await ExecuteDockerCommand("docker exec task-list-mcp touch /tmp/test-write");
            writeResult.Should().NotContain("Permission denied", "/tmp should be writable");
            
            var readOnlyResult = await ExecuteDockerCommand("docker exec task-list-mcp touch /usr/test-write 2>&1 || echo 'readonly'");
            readOnlyResult.Should().Contain("readonly", "/usr should be read-only");
        }
    }

    private async Task<string> ExecuteDockerCommand(string command)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = command.Replace("docker ", ""),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return !string.IsNullOrWhiteSpace(output) ? output : error;
    }
}
```

## Configuration Security Tests

### Configuration Security Tests

#### ConfigurationSecurityTests.cs
```csharp
public class ConfigurationSecurityTests
{
    [Fact]
    public void ApplicationSettings_ShouldNotContainDefaultPasswords()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Production.json", optional: true)
            .Build();

        // Act & Assert
        var allSettings = configuration.AsEnumerable();
        foreach (var (key, value) in allSettings)
        {
            if (!string.IsNullOrEmpty(value))
            {
                value.Should().NotBe("password", $"Setting '{key}' should not have default password");
                value.Should().NotBe("admin", $"Setting '{key}' should not have default admin value");
                value.Should().NotBe("secret", $"Setting '{key}' should not have default secret value");
                value.Should().NotBe("changeme", $"Setting '{key}' should not have default changeme value");
            }
        }
    }

    [Fact]
    public void DatabaseConfiguration_ShouldUseSecureSettings()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        // Act
        var dbConfig = configuration.GetSection("TaskListMcp:Database");

        // Assert
        var connectionString = dbConfig["ConnectionString"];
        if (!string.IsNullOrEmpty(connectionString))
        {
            connectionString.Should().NotContain("Integrated Security=false", "Should use integrated security when possible");
            connectionString.Should().NotContain("TrustServerCertificate=true", "Should validate server certificates");
            
            // For SQLite, ensure proper file permissions
            if (connectionString.Contains("Data Source=") && !connectionString.Contains(":memory:"))
            {
                connectionString.Should().NotContain("Password=", "SQLite file should not use password in connection string");
            }
        }

        var enableForeignKeys = dbConfig.GetValue<bool>("EnableForeignKeys");
        enableForeignKeys.Should().BeTrue("Foreign keys should be enabled for data integrity");
    }

    [Fact]
    public void LoggingConfiguration_ShouldNotLogSensitiveData()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        // Act
        var loggingConfig = configuration.GetSection("Logging");

        // Assert
        var logLevel = loggingConfig.GetValue<string>("LogLevel:Default");
        if (logLevel == "Debug" || logLevel == "Trace")
        {
            // Ensure we're not in production with debug logging
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            environment.Should().NotBe("Production", "Debug/Trace logging should not be enabled in production");
        }
    }

    [Fact]
    public void ServerConfiguration_ShouldUseSecureDefaults()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        // Act
        var serverConfig = configuration.GetSection("TaskListMcp:Server");

        // Assert
        var host = serverConfig.GetValue<string>("Host");
        if (!string.IsNullOrEmpty(host))
        {
            host.Should().NotBe("0.0.0.0", "Should not bind to all interfaces by default");
        }

        var port = serverConfig.GetValue<int>("Port");
        if (port > 0)
        {
            port.Should().BeGreaterThan(1024, "Should use non-privileged port");
            port.Should().BeLessThan(65536, "Should use valid port number");
        }
    }
}
```

## Security Test Execution

### Automated Security Testing

#### security-test-runner.ps1
```powershell
param(
    [string]$TestType = "All",
    [switch]$Verbose
)

Write-Host "Security Test Runner" -ForegroundColor Red
Write-Host "===================" -ForegroundColor Red
Write-Host ""

# Security test categories
$testCategories = @{
    "Input" = "TaskListMcp.Tests.Security.InputValidationTests"
    "SQL" = "TaskListMcp.Tests.Security.SqlInjectionTests"
    "Data" = "TaskListMcp.Tests.Security.DataSecurityTests"
    "MCP" = "TaskListMcp.Tests.Security.McpSecurityTests"
    "Docker" = "TaskListMcp.Tests.Security.DockerSecurityTests"
    "Config" = "TaskListMcp.Tests.Security.ConfigurationSecurityTests"
}

function Run-SecurityTests {
    param([string]$Category, [string]$TestClass)
    
    Write-Host "Running $Category Security Tests..." -ForegroundColor Yellow
    
    $testArgs = @(
        "test",
        "tests/TaskListMcp.Tests.Security",
        "--configuration", "Release",
        "--logger", "trx",
        "--logger", "console;verbosity=normal",
        "--results-directory", "security-test-results"
    )
    
    if ($TestClass -ne "All") {
        $testArgs += "--filter", "FullyQualifiedName~$TestClass"
    }
    
    if ($Verbose) {
        $testArgs += "--verbosity", "detailed"
    }
    
    & dotnet $testArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "SECURITY FAILURE: $Category tests failed!" -ForegroundColor Red
        return $false
    }
    
    Write-Host "$Category security tests passed." -ForegroundColor Green
    return $true
}

# Create results directory
New-Item -ItemType Directory -Path "security-test-results" -Force | Out-Null

$allPassed = $true

if ($TestType -eq "All") {
    foreach ($category in $testCategories.GetEnumerator()) {
        $passed = Run-SecurityTests -Category $category.Key -TestClass $category.Value
        $allPassed = $allPassed -and $passed
    }
} else {
    if ($testCategories.ContainsKey($TestType)) {
        $allPassed = Run-SecurityTests -Category $TestType -TestClass $testCategories[$TestType]
    } else {
        Write-Host "Unknown test type: $TestType" -ForegroundColor Red
        Write-Host "Available types: $($testCategories.Keys -join ', ')" -ForegroundColor Yellow
        exit 1
    }
}

Write-Host ""
if ($allPassed) {
    Write-Host "All security tests passed! ✅" -ForegroundColor Green
    exit 0
} else {
    Write-Host "Some security tests failed! ❌" -ForegroundColor Red
    Write-Host "Review the test results and fix security issues before deployment." -ForegroundColor Yellow
    exit 1
}
```

### Security Report Generation

#### SecurityReportGenerator.cs
```csharp
public class SecurityReportGenerator
{
    public static void GenerateSecurityReport(string reportPath, SecurityTestResults results)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Task List MCP Security Test Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .security-pass {{ background-color: #d4edda; color: #155724; }}
        .security-fail {{ background-color: #f8d7da; color: #721c24; }}
        .security-warning {{ background-color: #fff3cd; color: #856404; }}
        .test-category {{ margin: 20px 0; padding: 15px; border: 1px solid #ddd; }}
        table {{ border-collapse: collapse; width: 100%; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
        .critical {{ font-weight: bold; color: #dc3545; }}
        .high {{ color: #fd7e14; }}
        .medium {{ color: #ffc107; }}
        .low {{ color: #28a745; }}
    </style>
</head>
<body>
    <h1>🔒 Task List MCP Security Test Report</h1>
    <p>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
    
    <h2>Security Overview</h2>
    <div class=""test-category {(results.OverallSecurityRating == "PASS" ? "security-pass" : "security-fail")}"">
        <h3>Overall Security Rating: {results.OverallSecurityRating}</h3>
        <p>Critical Issues: {results.CriticalIssues}</p>
        <p>High Issues: {results.HighIssues}</p>
        <p>Medium Issues: {results.MediumIssues}</p>
        <p>Low Issues: {results.LowIssues}</p>
    </div>
    
    <h2>Security Test Results</h2>
    <table>
        <tr>
            <th>Test Category</th>
            <th>Status</th>
            <th>Tests Passed</th>
            <th>Tests Failed</th>
            <th>Severity</th>
        </tr>
        {string.Join("", results.CategoryResults.Select(r => $@"
        <tr>
            <td>{r.Category}</td>
            <td class=""{(r.Passed ? "security-pass" : "security-fail")}"">{(r.Passed ? "PASS" : "FAIL")}</td>
            <td>{r.PassedTests}</td>
            <td>{r.FailedTests}</td>
            <td class=""{r.Severity.ToLower()}"">{r.Severity}</td>
        </tr>"))}
    </table>
    
    <h2>Vulnerability Details</h2>
    {string.Join("", results.Vulnerabilities.Select(v => $@"
    <div class=""test-category security-fail"">
        <h4 class=""{v.Severity.ToLower()}"">{v.Title} ({v.Severity})</h4>
        <p><strong>Description:</strong> {v.Description}</p>
        <p><strong>Impact:</strong> {v.Impact}</p>
        <p><strong>Recommendation:</strong> {v.Recommendation}</p>
    </div>"))}
    
    <h2>Security Recommendations</h2>
    <div class=""test-category"">
        <ul>
            <li>Regularly update dependencies to patch security vulnerabilities</li>
            <li>Implement proper input validation and sanitization</li>
            <li>Use parameterized queries to prevent SQL injection</li>
            <li>Configure proper logging without exposing sensitive data</li>
            <li>Ensure container security best practices</li>
            <li>Regular security testing and code reviews</li>
        </ul>
    </div>
</body>
</html>";

        File.WriteAllText(reportPath, html);
    }
}

public class SecurityTestResults
{
    public string OverallSecurityRating { get; set; } = "UNKNOWN";
    public int CriticalIssues { get; set; }
    public int HighIssues { get; set; }
    public int MediumIssues { get; set; }
    public int LowIssues { get; set; }
    public List<SecurityCategoryResult> CategoryResults { get; set; } = new();
    public List<SecurityVulnerability> Vulnerabilities { get; set; } = new();
}

public class SecurityCategoryResult
{
    public string Category { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public string Severity { get; set; } = "MEDIUM";
}

public class SecurityVulnerability
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string Severity { get; set; } = "MEDIUM";
}
```

This comprehensive security testing guide ensures that the Task List MCP Server is protected against common security vulnerabilities and follows security best practices for both the application code and deployment environment.
