using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP Tool for moving tasks between lists
/// </summary>
[McpServerToolType]
public static class MoveTaskTool
{
    [McpServerTool]
    [Description("Moves a task to a different list or removes it from lists")]
    public static async Task<object> MoveTaskAsync(
        [Description("ID of the task to move")] int taskId,
        [Description("Target list ID (null to remove from any list)")] int? targetListId = null,
        ListService listService = null!,
        TaskService taskService = null!,
        ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("Moving task {TaskId} to list {TargetListId}", taskId, targetListId);

            // First check if the task exists
            var task = await taskService.GetTaskByIdAsync(taskId);
            if (task == null)
            {
                logger?.LogWarning("Task {TaskId} not found for move operation", taskId);
                return new
                {
                    success = false,
                    error = "not_found",
                    message = $"Task with ID {taskId} not found"
                };
            }

            var moved = await listService.MoveTaskToListAsync(taskId, targetListId);

            if (moved)
            {
                logger?.LogInformation("Successfully moved task {TaskId} to list {TargetListId}", taskId, targetListId);

                // Get the updated task to return current state
                var updatedTask = await taskService.GetTaskByIdAsync(taskId);

                return new
                {
                    success = true,
                    task = new
                    {
                        id = updatedTask!.Id,
                        title = updatedTask.Title,
                        listId = updatedTask.ListId,
                        listName = updatedTask.ListName,
                        status = updatedTask.Status.ToString(),
                        updatedAt = updatedTask.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC")
                    },
                    message = targetListId.HasValue 
                        ? $"Task '{updatedTask.Title}' moved to list {targetListId}" 
                        : $"Task '{updatedTask.Title}' removed from all lists"
                };
            }
            else
            {
                logger?.LogWarning("Failed to move task {TaskId}", taskId);
                return new
                {
                    success = false,
                    error = "move_failed",
                    message = "Failed to move the task"
                };
            }
        }
        catch (ArgumentException ex)
        {
            logger?.LogWarning("Validation error moving task {TaskId}: {Error}", taskId, ex.Message);
            return new
            {
                success = false,
                error = "validation_error",
                message = ex.Message
            };
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unexpected error moving task {TaskId}", taskId);
            return new
            {
                success = false,
                error = "internal_error",
                message = "An unexpected error occurred while moving the task"
            };
        }
    }
}
