using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for creating tags
/// </summary>
[McpServerToolType]
public class CreateTagTool
{
    /// <summary>
    /// Creates a new tag with optional color and parent
    /// </summary>
    [McpServerTool(Name = "create_tag")]
    [Description("Creates a new tag with optional color and parent for hierarchical organization")]
    public static async Task<object> CreateTagAsync(
        [Description("The name of the tag (must be unique)")] string name,
        [Description("Optional hex color code for the tag (e.g., #FF5733)")] string? color = null,
        [Description("Optional parent tag ID for hierarchical tags")] int? parentId = null,
        TagService tagService = null!,
        ILogger<CreateTagTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Creating tag: {TagName}", name);

            var tag = await tagService.CreateTagAsync(name, color, parentId);

            if (tag == null)
            {
                logger.LogWarning("Failed to create tag: {TagName}", name);
                return new { success = false, error = $"Failed to create tag '{name}'. Tag name may already exist or parent tag not found." };
            }

            logger.LogInformation("Successfully created tag: {TagName} with ID: {TagId}", name, tag.Id);

            return new
            {
                success = true,
                message = $"Tag '{tag.Name}' created successfully",
                tag = new
                {
                    id = tag.Id,
                    name = tag.Name,
                    color = tag.Color,
                    parentId = tag.ParentId,
                    path = tag.GetPath(),
                    depth = tag.GetDepth(),
                    createdAt = tag.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating tag: {TagName}", name);
            return new { success = false, error = $"Error creating tag: {ex.Message}" };
        }
    }
}
