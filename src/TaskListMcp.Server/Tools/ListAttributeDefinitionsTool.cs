using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for listing attribute definitions
/// </summary>
[McpServerToolType]
public class ListAttributeDefinitionsTool
{
    /// <summary>
    /// List all attribute definitions
    /// </summary>
    [McpServerTool(Name = "list_attribute_definitions")]
    [Description("List all custom attribute definitions")]
    public static async Task<object> ListAttributeDefinitionsAsync(
        AttributeService attributeService = null!,
        ILogger<ListAttributeDefinitionsTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Listing all attribute definitions");
            var definitions = await attributeService.GetAllAttributeDefinitionsAsync();

            return new
            {
                success = true,
                message = $"Found {definitions.Count} attribute definitions",
                definitions = definitions.Select(def => new
                {
                    id = def.Id,
                    name = def.Name,
                    type = def.Type.ToString(),
                    isRequired = def.IsRequired,
                    defaultValue = def.DefaultValue,
                    validationRules = def.ValidationRules,
                    createdAt = def.CreatedAt
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing attribute definitions");
            return new
            {
                success = false,
                error = ex.Message
            };
        }
    }
}
