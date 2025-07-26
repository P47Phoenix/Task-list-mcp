using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for listing tasks
/// </summary>
[McpServerToolType]
public class ListTasksTool
{
    /// <summary>
    /// Lists tasks with optional filtering
    /// </summary>
    [McpServerTool(Name = "list_tasks")]
    [Description("Lists tasks with optional filtering by status and list ID")]
    public static async Task<object> ListTasksAsync(
        [Description("Optional list ID to filter by")] int? listId = null,
        [Description("Optional status to filter by (Pending, InProgress, Completed, Cancelled, Blocked)")] string? status = null,
        [Description("Optional limit for pagination (default 50)")] int? limit = null,
        [Description("Optional offset for pagination (default 0)")] int? offset = null,
        TaskService taskService = null!,
        ILogger<ListTasksTool> logger = null!)
    {
        try
        {
            Models.TaskStatus? statusParsed = null;
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<Models.TaskStatus>(status, true, out var parsedStatus))
                {
                    statusParsed = parsedStatus;
                }
                else
                {
                    throw new ArgumentException($"Invalid status: {status}. Valid values are: Pending, InProgress, Completed, Cancelled, Blocked");
                }
            }

            var tasks = await taskService.ListTasksAsync(
                listId: listId,
                status: statusParsed,
                limit: limit,
                offset: offset);

            logger.LogInformation("Listed {TaskCount} tasks", tasks.Count);

            return new
            {
                success = true,
                tasks = tasks.Select(task => new
                {
                    id = task.Id,
                    title = task.Title,
                    description = task.Description,
                    status = task.Status.ToString(),
                    listId = task.ListId,
                    createdAt = task.CreatedAt,
                    updatedAt = task.UpdatedAt,
                    dueDate = task.DueDate,
                    priority = task.Priority,
                    estimatedHours = task.EstimatedHours
                }).ToList(),
                count = tasks.Count
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list tasks");
            return new
            {
                success = false,
                error = ex.Message
            };
        }
    }
}
