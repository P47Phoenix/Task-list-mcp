using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for retrieving task details
/// </summary>
[McpServerToolType]
public class GetTaskTool
{
    /// <summary>
    /// Gets a task by ID with all related data
    /// </summary>
    [McpServerTool(Name = "get_task")]
    [Description("Gets a task by ID with all related data")]
    public static async Task<object> GetTaskAsync(
        [Description("The ID of the task to retrieve")]
        int taskId,
        TaskService taskService = null!,
        ILogger<GetTaskTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Retrieving task with ID: {TaskId}", taskId);

            var task = await taskService.GetTaskByIdAsync(taskId);
            
            if (task == null)
            {
                logger.LogWarning("Task with ID {TaskId} not found", taskId);
                return new { success = false, error = $"Task with ID {taskId} not found" };
            }

            logger.LogInformation("Successfully retrieved task: {TaskTitle}", task.Title);
            
            return new { 
                success = true, 
                task = new
                {
                    id = task.Id,
                    title = task.Title,
                    description = task.Description,
                    status = task.Status.ToString(),
                    listId = task.ListId,
                    listName = task.ListName,
                    createdAt = task.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    updatedAt = task.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    deletedAt = task.DeletedAt?.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving task with ID: {TaskId}", taskId);
            return new { success = false, error = $"Error retrieving task: {ex.Message}" };
        }
    }
}
