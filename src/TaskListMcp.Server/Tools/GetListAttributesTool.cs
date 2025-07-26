using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for getting list attributes
/// </summary>
[McpServerToolType]
public class GetListAttributesTool
{
    /// <summary>
    /// Get all custom attributes for a task list
    /// </summary>
    [McpServerTool(Name = "get_list_attributes")]
    [Description("Get all custom attributes for a task list")]
    public static async Task<object> GetListAttributesAsync(
        [Description("ID of the task list")] int listId,
        AttributeService attributeService = null!,
        ILogger<GetListAttributesTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Getting attributes for list: {ListId}", listId);
            var attributes = await attributeService.GetListAttributesAsync(listId);

            return new
            {
                success = true,
                message = $"Found {attributes.Count} attributes for list {listId}",
                listId = listId,
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
            logger.LogError(ex, "Error getting list attributes");
            return new
            {
                success = false,
                error = ex.Message
            };
        }
    }
}
