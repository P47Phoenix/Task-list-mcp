using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for deleting tasks
/// </summary>
[McpServerToolType]
public class DeleteTaskTool
{
    /// <summary>
    /// Deletes a task by ID with soft delete functionality
    /// </summary>
    [McpServerTool(Name = "delete_task")]
    [Description("Deletes a task by ID with soft delete functionality")]
    public static async Task<object> DeleteTaskAsync(
        [Description("The ID of the task to delete")]
        int taskId,
        TaskService taskService = null!,
        ILogger<DeleteTaskTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Attempting to delete task with ID: {TaskId}", taskId);

            // Check if task exists
            var existingTask = await taskService.GetTaskByIdAsync(taskId);
            if (existingTask == null)
            {
                logger.LogWarning("Task with ID {TaskId} not found", taskId);
                return new { success = false, error = $"Task with ID {taskId} not found" };
            }

            // Delete the task (soft delete)
            var success = await taskService.DeleteTaskAsync(taskId);
            
            if (success)
            {
                logger.LogInformation("Successfully deleted task with ID: {TaskId}", taskId);
                return new { 
                    success = true, 
                    message = $"Task '{existingTask.Title}' has been deleted",
                    taskId = taskId
                };
            }
            else
            {
                logger.LogError("Failed to delete task with ID: {TaskId}", taskId);
                return new { success = false, error = "Failed to delete task" };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting task with ID: {TaskId}", taskId);
            return new { success = false, error = $"Error deleting task: {ex.Message}" };
        }
    }
}
