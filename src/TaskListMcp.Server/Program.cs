using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.Sqlite;
using ModelContextProtocol.Server;
using TaskListMcp.Models;
using TaskListMcp.Data;
using TaskListMcp.Core.Services;
using TaskListMcp.Server.Tools;
using TaskListMcp.Server.Configuration;

namespace TaskListMcp.Server;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services, configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Get logger
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Starting TaskList MCP Server...");

            // Get configuration
            var config = serviceProvider.GetRequiredService<TaskListMcpConfig>();
            logger.LogInformation("Server: {ServerName} v{Version}", config.Server.Name, config.Server.Version);

            // Initialize database
            var databaseManager = serviceProvider.GetRequiredService<DatabaseManager>();
            await databaseManager.InitializeDatabaseAsync();
            logger.LogInformation("Database initialized successfully");

            // Start health check web server in background
            var webHostTask = StartWebHostAsync(serviceProvider, logger);

            // Create MCP server
            logger.LogInformation("Starting MCP server...");
            
            var mcpOptions = new McpServerOptions
            {
                ServerInfo = new()
                {
                    Name = config.Server.Name,
                    Version = config.Server.Version
                },
                Capabilities = new()
                {
                    Tools = new()
                    {
                        ToolCollection = new()
                        {
                            // Task management tools
                            McpServerTool.Create(CreateTaskTool.CreateTaskAsync, new() { Services = serviceProvider }),
                            McpServerTool.Create(UpdateTaskTool.UpdateTaskAsync, new() { Services = serviceProvider }),
                            McpServerTool.Create(ListTasksTool.ListTasksAsync, new() { Services = serviceProvider }),
                            McpServerTool.Create(MoveTaskTool.MoveTaskAsync, new() { Services = serviceProvider }),
                            
                            // List management tools
                            McpServerTool.Create(CreateListTool.CreateListAsync, new() { Services = serviceProvider }),
                            McpServerTool.Create(UpdateListTool.UpdateListAsync, new() { Services = serviceProvider }),
                            McpServerTool.Create(DeleteListTool.DeleteListAsync, new() { Services = serviceProvider }),
                            McpServerTool.Create(GetListTool.GetListAsync, new() { Services = serviceProvider }),
                            McpServerTool.Create(ListAllListsTool.ListAllListsAsync, new() { Services = serviceProvider })
                        }
                    }
                }
            };

            await using var server = McpServerFactory.Create(
                new StdioServerTransport("TaskListMcp"), 
                mcpOptions, 
                serviceProvider.GetService<ILoggerFactory>(),
                serviceProvider);
            
            logger.LogInformation("MCP server started successfully. Listening for requests...");
            await server.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add configuration
        services.AddSingleton(configuration);

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        // Bind configuration
        var config = configuration.GetSection("TaskListMcp").Get<TaskListMcpConfig>() ?? new TaskListMcpConfig();
        services.AddSingleton(config);
        services.AddSingleton(config.Database);
        services.AddSingleton(config.Server);
        services.AddSingleton(config.Features);

        // Add configuration options
        services.Configure<TaskListMcpOptions>(configuration.GetSection(TaskListMcpOptions.SectionName));

        // Add controllers for health endpoints
        services.AddControllers();
        services.AddHealthChecks();

        // Add database services with connection pooling
        var connectionString = config.Database.ConnectionString;
        
        // Enhance connection string with pooling and optimization
        if (!connectionString.Contains("Pooling="))
        {
            var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString)
            {
                Pooling = true,
                Cache = Microsoft.Data.Sqlite.SqliteCacheMode.Shared,
                ForeignKeys = config.Database.EnableForeignKeys,
                DefaultTimeout = 30
            };
            connectionString = builder.ToString();
        }
        
        services.AddSingleton<IDbConnectionFactory>(provider => 
            new SqliteConnectionFactory(connectionString));
        services.AddScoped<DatabaseManager>();

        // Add business logic services
        services.AddScoped<TaskService>();
        services.AddScoped<ListService>();
        // TODO: Update these services to use IDbConnectionFactory instead of DatabaseManager
        // services.AddScoped<TemplateService>();
        // services.AddScoped<TagService>();
        // services.AddScoped<AttributeService>();
        // services.AddScoped<SearchService>();

        // MCP tools are automatically discovered via attributes
        // No need to manually register them with the new SDK
    }

    private static async Task StartWebHostAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            
            var builder = WebApplication.CreateBuilder();
            
            // Configure services for web host
            ConfigureServices(builder.Services, configuration);

            var app = builder.Build();

            // Configure HTTP pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.MapControllers();
            app.MapHealthChecks("/health");

            logger.LogInformation("Health check server starting on http://localhost:8080");
            
            // Run web server in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await app.RunAsync("http://localhost:8080");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Health check server failed");
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start health check server");
        }
    }
}
