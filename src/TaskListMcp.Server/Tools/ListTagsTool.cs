using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for listing all tags
/// </summary>
[McpServerToolType]
public class ListTagsTool
{
    /// <summary>
    /// Lists all tags with usage statistics and optional hierarchy
    /// </summary>
    [McpServerTool(Name = "list_tags")]
    [Description("Lists all tags with usage statistics and optional hierarchical structure")]
    public static async Task<object> ListTagsAsync(
        [Description("Whether to include hierarchical structure (default: false)")] bool includeHierarchy = false,
        TagService tagService = null!,
        ILogger<ListTagsTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Listing tags with hierarchy: {IncludeHierarchy}", includeHierarchy);

            var tags = await tagService.GetAllTagsAsync(includeHierarchy);

            logger.LogInformation("Found {TagCount} tags", tags.Count);

            return new
            {
                success = true,
                count = tags.Count,
                includeHierarchy = includeHierarchy,
                tags = tags.Select(t => new
                {
                    id = t.Id,
                    name = t.Name,
                    color = t.Color,
                    parentId = t.ParentId,
                    path = t.GetPath(),
                    depth = t.GetDepth(),
                    taskCount = t.TaskCount,
                    listCount = t.ListCount,
                    createdAt = t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    children = includeHierarchy ? t.Children.Select(c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        color = c.Color,
                        path = c.GetPath()
                    }).ToList() : null
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing tags");
            return new { success = false, error = $"Error listing tags: {ex.Message}" };
        }
    }
}
