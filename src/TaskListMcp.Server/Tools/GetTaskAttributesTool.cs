using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for getting task attributes
/// </summary>
[McpServerToolType]
public class GetTaskAttributesTool
{
    /// <summary>
    /// Get all custom attributes for a task
    /// </summary>
    [McpServerTool(Name = "get_task_attributes")]
    [Description("Get all custom attributes for a task")]
    public static async Task<object> GetTaskAttributesAsync(
        [Description("ID of the task")] int taskId,
        AttributeService attributeService = null!,
        ILogger<GetTaskAttributesTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Getting attributes for task: {TaskId}", taskId);
            var attributes = await attributeService.GetTaskAttributesAsync(taskId);

            return new
            {
                success = true,
                message = $"Found {attributes.Count} attributes for task {taskId}",
                taskId = taskId,
                attributes = attributes.Select(attr => new
                {
                    attributeName = attr.AttributeDefinition?.Name,
                    attributeType = attr.AttributeDefinition?.Type.ToString(),
                    value = attr.Value,
                    isRequired = attr.AttributeDefinition?.IsRequired,
                    createdAt = attr.CreatedAt,
                    updatedAt = attr.UpdatedAt
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting task attributes");
            return new
            {
                success = false,
                error = ex.Message
            };
        }
    }
}
