using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for deleting attribute definitions
/// </summary>
[McpServerToolType]
public class DeleteAttributeDefinitionTool
{
    /// <summary>
    /// Delete an attribute definition and all its values
    /// </summary>
    [McpServerTool(Name = "delete_attribute_definition")]
    [Description("Delete an attribute definition and all its values from tasks and lists")]
    public static async Task<object> DeleteAttributeDefinitionAsync(
        [Description("Name of the attribute definition to delete")] string attributeName,
        AttributeService attributeService = null!,
        ILogger<DeleteAttributeDefinitionTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Deleting attribute definition: {AttributeName}", attributeName);

            // Find the attribute definition by name
            var definition = await attributeService.GetAttributeDefinitionByNameAsync(attributeName);
            if (definition == null)
            {
                throw new ArgumentException($"Attribute definition '{attributeName}' not found");
            }

            var deleted = await attributeService.DeleteAttributeDefinitionAsync(definition.Id);

            if (deleted)
            {
                return new
                {
                    success = true,
                    message = $"Deleted attribute definition '{attributeName}' and all its values"
                };
            }
            else
            {
                return new
                {
                    success = false,
                    message = $"Attribute definition '{attributeName}' not found"
                };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting attribute definition");
            return new
            {
                success = false,
                error = ex.Message
            };
        }
    }
}
