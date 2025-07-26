using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for creating tasks
/// </summary>
[McpServerToolType]
public class CreateTaskTool
{
    /// <summary>
    /// Creates a new task with title, description, and optional metadata
    /// </summary>
    [McpServerTool(Name = "create_task")]
    [Description("Creates a new task with title, description, and optional metadata")]
    public static async Task<object> CreateTaskAsync(
        [Description("The title of the task")] string title,
        [Description("Optional description of the task")] string? description = null,
        [Description("Optional list ID (defaults to 1)")] int listId = 1,
        [Description("Optional due date (ISO 8601 format)")] string? dueDate = null,
        [Description("Optional priority (0-5, default 0)")] int priority = 0,
        [Description("Optional estimated hours")] decimal? estimatedHours = null,
        TaskService taskService = null!,
        ILogger<CreateTaskTool> logger = null!)
    {
        try
        {
            DateTime? dueDateParsed = null;
            if (!string.IsNullOrEmpty(dueDate))
            {
                dueDateParsed = DateTime.Parse(dueDate);
            }

            var task = await taskService.CreateTaskAsync(
                title: title,
                description: description,
                listId: listId,
                dueDate: dueDateParsed,
                priority: priority,
                estimatedHours: estimatedHours);

            logger.LogInformation("Created task: {TaskId} - {Title}", task.Id, task.Title);

            return new
            {
                success = true,
                task = new
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
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create task: {Title}", title);
            return new
            {
                success = false,
                error = ex.Message
            };
        }
    }
}
