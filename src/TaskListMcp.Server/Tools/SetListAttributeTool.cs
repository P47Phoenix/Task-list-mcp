using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for setting list attributes
/// </summary>
[McpServerToolType]
public class SetListAttributeTool
{
    /// <summary>
    /// Set a custom attribute value for a task list
    /// </summary>
    [McpServerTool(Name = "set_list_attribute")]
    [Description("Set a custom attribute value for a task list")]
    public static async Task<object> SetListAttributeAsync(
        [Description("ID of the task list")] int listId,
        [Description("Name of the attribute definition")] string attributeName,
        [Description("Value to set for the attribute")] string value,
        AttributeService attributeService = null!,
        ILogger<SetListAttributeTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Setting list attribute: ListId={ListId}, Attribute={AttributeName}, Value={Value}", 
                listId, attributeName, value);

            // Find the attribute definition by name
            var definition = await attributeService.GetAttributeDefinitionByNameAsync(attributeName);
            if (definition == null)
            {
                throw new ArgumentException($"Attribute definition '{attributeName}' not found");
            }

            await attributeService.SetListAttributeAsync(listId, definition.Id, value);

            return new
            {
                success = true,
                message = $"Set attribute '{attributeName}' = '{value}' for list {listId}"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting list attribute");
            return new
            {
                success = false,
                error = ex.Message
            };
        }
    }
}
