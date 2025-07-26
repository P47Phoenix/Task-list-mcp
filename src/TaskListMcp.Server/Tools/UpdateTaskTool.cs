using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for updating tasks
/// </summary>
[McpServerToolType]
public class UpdateTaskTool
{
    /// <summary>
    /// Updates an existing task's properties
    /// </summary>
    [McpServerTool(Name = "update_task")]
    [Description("Updates an existing task's properties")]
    public static async Task<object> UpdateTaskAsync(
        [Description("The ID of the task to update")] int id,
        [Description("Optional new title")] string? title = null,
        [Description("Optional new description")] string? description = null,
        [Description("Optional new status (Pending, InProgress, Completed, Cancelled, Blocked)")] string? status = null,
        [Description("Optional new due date (ISO 8601 format)")] string? dueDate = null,
        [Description("Optional new priority (Normal, Low, High, Critical)")] string? priority = null,
        [Description("Optional new estimated hours")] decimal? estimatedHours = null,
        TaskService taskService = null!,
        ILogger<UpdateTaskTool> logger = null!)
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

            Priority? priorityParsed = null;
            if (!string.IsNullOrEmpty(priority))
            {
                if (Enum.TryParse<Priority>(priority, true, out var parsedPriority))
                {
                    priorityParsed = parsedPriority;
                }
                else
                {
                    throw new ArgumentException($"Invalid priority: {priority}. Valid values are: Normal, Low, High, Critical");
                }
            }

            DateTime? dueDateParsed = null;
            if (!string.IsNullOrEmpty(dueDate))
            {
                dueDateParsed = DateTime.Parse(dueDate);
            }

            var task = await taskService.UpdateTaskAsync(
                taskId: id,
                title: title,
                description: description,
                status: statusParsed,
                dueDate: dueDateParsed,
                priority: priorityParsed,
                estimatedHours: estimatedHours);

            logger.LogInformation("Updated task: {TaskId} - {Title}", task.Id, task.Title);

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
            logger.LogError(ex, "Failed to update task: {TaskId}", id);
            return new
            {
                success = false,
                error = ex.Message
            };
        }
    }
}
