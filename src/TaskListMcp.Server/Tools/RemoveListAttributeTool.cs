using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for removing list attributes
/// </summary>
[McpServerToolType]
public class RemoveListAttributeTool
{
    /// <summary>
    /// Remove a custom attribute from a task list
    /// </summary>
    [McpServerTool(Name = "remove_list_attribute")]
    [Description("Remove a custom attribute from a task list")]
    public static async Task<object> RemoveListAttributeAsync(
        [Description("ID of the task list")] int listId,
        [Description("Name of the attribute to remove")] string attributeName,
        AttributeService attributeService = null!,
        ILogger<RemoveListAttributeTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Removing list attribute: ListId={ListId}, Attribute={AttributeName}", listId, attributeName);

            // Find the attribute definition by name
            var definition = await attributeService.GetAttributeDefinitionByNameAsync(attributeName);
            if (definition == null)
            {
                throw new ArgumentException($"Attribute definition '{attributeName}' not found");
            }

            var removed = await attributeService.RemoveListAttributeAsync(listId, definition.Id);

            if (removed)
            {
                return new
                {
                    success = true,
                    message = $"Removed attribute '{attributeName}' from list {listId}"
                };
            }
            else
            {
                return new
                {
                    success = false,
                    message = $"List {listId} does not have attribute '{attributeName}'"
                };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing list attribute");
            return new
            {
                success = false,
                error = ex.Message
            };
        }
    }
}
