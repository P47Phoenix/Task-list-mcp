using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskListMcp.Core.Services;
using TaskListMcp.Data;
using TaskListMcp.Models;

namespace TaskListMcp.Tests.Unit.Helpers;

/// <summary>
/// Test fixture providing database and service setup for unit tests
/// </summary>
public class TestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }
    public DatabaseManager DatabaseManager { get; private set; }
    private readonly ILogger<TestFixture>? _logger;
    private readonly string _tempDatabasePath;

    public TestFixture()
    {
        // Create a temporary database file for this test run
        _tempDatabasePath = Path.Combine(Path.GetTempPath(), $"test_tasklistmcp_{Guid.NewGuid():N}.db");
        
        var services = new ServiceCollection();
        
        // Configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json", optional: true)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TaskListMcp:Database:ConnectionString"] = $"Data Source={_tempDatabasePath}",
                ["TaskListMcp:Database:EnableForeignKeys"] = "true",
                ["TaskListMcp:Database:EnableWAL"] = "false"
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        // Use a standard connection factory pointing to our temp database
        services.AddSingleton<IDbConnectionFactory>(provider => 
            new SqliteConnectionFactory($"Data Source={_tempDatabasePath}"));
        
        services.AddScoped<DatabaseManager>();
        
        // Core services
        services.AddScoped<TaskService>();

        ServiceProvider = services.BuildServiceProvider();
        
        // Get logger
        _logger = ServiceProvider.GetService<ILogger<TestFixture>>();
        
        // Initialize database
        DatabaseManager = ServiceProvider.GetRequiredService<DatabaseManager>();
        DatabaseManager.InitializeDatabaseAsync().Wait();
    }

    /// <summary>
    /// Creates a new service scope for testing
    /// </summary>
    public IServiceScope CreateScope()
    {
        return ServiceProvider.CreateScope();
    }

    /// <summary>
    /// Gets a service from the test container
    /// </summary>
    public T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Resets the database to a clean state
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var connection = await DatabaseManager.GetConnectionAsync();
        
        // Clear all tables (order matters for foreign key constraints)
        var deleteCommands = new[]
        {
            "DELETE FROM task_attributes",
            "DELETE FROM list_attributes", 
            "DELETE FROM task_tags",
            "DELETE FROM list_tags",
            "DELETE FROM template_tasks",
            "DELETE FROM tasks",
            "DELETE FROM task_lists WHERE id > 1",
            "DELETE FROM templates",
            "DELETE FROM tags",
            "DELETE FROM attribute_definitions"
        };

        foreach (var commandText in deleteCommands)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = commandText;
                await command.ExecuteNonQueryAsync();
            }
            catch (SqliteException ex) when (ex.Message.Contains("no such table"))
            {
                // Ignore table not found errors - tables might not exist yet
                _logger?.LogDebug($"Table not found during reset: {commandText}");
            }
        }

        // Reset auto-increment counters
        var resetCommands = new[]
        {
            "UPDATE sqlite_sequence SET seq = 1 WHERE name = 'tasks'",
            "UPDATE sqlite_sequence SET seq = 1 WHERE name = 'task_lists'", 
            "UPDATE sqlite_sequence SET seq = 0 WHERE name = 'templates'",
            "UPDATE sqlite_sequence SET seq = 0 WHERE name = 'tags'",
            "UPDATE sqlite_sequence SET seq = 0 WHERE name = 'attribute_definitions'"
        };

        foreach (var commandText in resetCommands)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = commandText;
                await command.ExecuteNonQueryAsync();
            }
            catch (SqliteException)
            {
                // Ignore errors - sequence might not exist
            }
        }

        // Ensure default list exists
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR IGNORE INTO task_lists (id, name, description, created_at, updated_at)
                VALUES (1, 'Default', 'Default task list for new tasks', datetime('now'), datetime('now'))";
            await command.ExecuteNonQueryAsync();
        }
        catch (SqliteException ex)
        {
            _logger?.LogWarning($"Could not ensure default list exists: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposableServiceProvider)
        {
            disposableServiceProvider.Dispose();
        }
        
        // Clean up the temporary database file
        try
        {
            if (File.Exists(_tempDatabasePath))
            {
                File.Delete(_tempDatabasePath);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"Could not delete temporary database file: {ex.Message}");
        }
    }
}
