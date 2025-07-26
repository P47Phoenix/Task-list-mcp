using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TaskListMcp.Core;
using TaskListMcp.Data;
using System.Diagnostics;

namespace TaskListMcp.Server.Controllers;

/// <summary>
/// Health check controller for monitoring server status
/// </summary>
[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly DatabaseManager _databaseManager;
    private readonly ILogger<HealthController> _logger;

    public HealthController(DatabaseManager databaseManager, ILogger<HealthController> logger)
    {
        _databaseManager = databaseManager;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            // Test database connectivity
            using var connection = await _databaseManager.GetConnectionAsync();
            
            var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync();

            var response = new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                database = "connected"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            
            var response = new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                database = "disconnected",
                error = ex.Message
            };

            return StatusCode(503, response);
        }
    }

    /// <summary>
    /// Detailed health check with metrics
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailed()
    {
        try
        {
            using var connection = await _databaseManager.GetConnectionAsync();
            
            // Get database statistics
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT 
                    (SELECT COUNT(*) FROM tasks WHERE deleted_at IS NULL) as task_count,
                    (SELECT COUNT(*) FROM task_lists WHERE deleted_at IS NULL) as list_count,
                    (SELECT COUNT(*) FROM templates) as template_count,
                    (SELECT COUNT(*) FROM tags) as tag_count";
                    
            var reader = await command.ExecuteReaderAsync();
            
            int taskCount = 0, listCount = 0, templateCount = 0, tagCount = 0;
            
            if (await reader.ReadAsync())
            {
                taskCount = reader.GetInt32(0);
                listCount = reader.GetInt32(1);
                templateCount = reader.GetInt32(2);
                tagCount = reader.GetInt32(3);
            }

            var response = new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                database = new
                {
                    status = "connected",
                    tasks = taskCount,
                    lists = listCount,
                    templates = templateCount,
                    tags = tagCount
                },
                uptime = DateTime.UtcNow.Subtract(Process.GetCurrentProcess().StartTime),
                memory = new
                {
                    workingSet = GC.GetTotalMemory(false),
                    gcCollections = new
                    {
                        gen0 = GC.CollectionCount(0),
                        gen1 = GC.CollectionCount(1),
                        gen2 = GC.CollectionCount(2)
                    }
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detailed health check failed");
            
            var response = new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                error = ex.Message
            };

            return StatusCode(503, response);
        }
    }
}
