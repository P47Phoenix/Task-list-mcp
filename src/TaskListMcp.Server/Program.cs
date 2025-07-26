using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Models;
using TaskListMcp.Data;
using TaskListMcp.Core.Services;
using TaskListMcp.Server.Tools;

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

        // Add database manager
        services.AddSingleton<DatabaseManager>();

        // Add business logic services
        services.AddScoped<TaskService>();
        services.AddScoped<ListService>();
        services.AddScoped<TemplateService>();
        services.AddScoped<TagService>();
        services.AddScoped<AttributeService>();
        services.AddScoped<SearchService>();

        // MCP tools are automatically discovered via attributes
        // No need to manually register them with the new SDK
    }
}
