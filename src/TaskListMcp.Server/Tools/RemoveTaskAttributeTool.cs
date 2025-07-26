using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for removing task attributes
/// </summary>
[McpServerToolType]
public class RemoveTaskAttributeTool
{
    /// <summary>
    /// Remove a custom attribute from a task
    /// </summary>
    [McpServerTool(Name = "remove_task_attribute")]
    [Description("Remove a custom attribute from a task")]
    public static async Task<object> RemoveTaskAttributeAsync(
        [Description("ID of the task")] int taskId,
        [Description("Name of the attribute to remove")] string attributeName,
        AttributeService attributeService = null!,
        ILogger<RemoveTaskAttributeTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Removing task attribute: TaskId={TaskId}, Attribute={AttributeName}", taskId, attributeName);

            // Find the attribute definition by name
            var definition = await attributeService.GetAttributeDefinitionByNameAsync(attributeName);
            if (definition == null)
            {
                throw new ArgumentException($"Attribute definition '{attributeName}' not found");
            }

            var removed = await attributeService.RemoveTaskAttributeAsync(taskId, definition.Id);

            if (removed)
            {
                return new
                {
                    success = true,
                    message = $"Removed attribute '{attributeName}' from task {taskId}"
                };
            }
            else
            {
                return new
                {
                    success = false,
                    message = $"Task {taskId} does not have attribute '{attributeName}'"
                };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing task attribute");
            return new
            {
                success = false,
                error = ex.Message
            };
        }
    }
}
