using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for setting task attributes
/// </summary>
[McpServerToolType]
public class SetTaskAttributeTool
{
    /// <summary>
    /// Set a custom attribute value for a task
    /// </summary>
    [McpServerTool(Name = "set_task_attribute")]
    [Description("Set a custom attribute value for a task")]
    public static async Task<object> SetTaskAttributeAsync(
        [Description("ID of the task")] int taskId,
        [Description("Name of the attribute definition")] string attributeName,
        [Description("Value to set for the attribute")] string value,
        AttributeService attributeService = null!,
        ILogger<SetTaskAttributeTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Setting task attribute: TaskId={TaskId}, Attribute={AttributeName}, Value={Value}", 
                taskId, attributeName, value);

            // Find the attribute definition by name
            var definition = await attributeService.GetAttributeDefinitionByNameAsync(attributeName);
            if (definition == null)
            {
                throw new ArgumentException($"Attribute definition '{attributeName}' not found");
            }

            await attributeService.SetTaskAttributeAsync(taskId, definition.Id, value);

            return new
            {
                success = true,
                message = $"Set attribute '{attributeName}' = '{value}' for task {taskId}"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting task attribute");
            return new
            {
                success = false,
                error = ex.Message
            };
        }
    }
}
