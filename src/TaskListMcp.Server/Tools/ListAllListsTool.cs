using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP Tool for listing all task lists
/// </summary>
[McpServerToolType]
public static class ListAllListsTool
{
    [McpServerTool]
    [Description("Lists all task lists with optional hierarchical display")]
    public static async Task<object> ListAllListsAsync(
        [Description("Whether to return lists in hierarchical structure (default: false)")] bool hierarchical = false,
        ListService listService = null!,
        ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("Listing all lists with hierarchical={Hierarchical}", hierarchical);

            var lists = await listService.GetAllListsAsync(hierarchical);

            logger?.LogInformation("Successfully retrieved {Count} lists", lists.Count);

            object FormatList(Models.TaskList list)
            {
                return new
                {
                    id = list.Id,
                    name = list.Name,
                    description = list.Description,
                    parentListId = list.ParentListId,
                    parentListName = list.ParentListName,
                    taskCount = list.TaskCount,
                    childListCount = list.ChildListCount,
                    createdAt = list.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    updatedAt = list.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    path = list.GetPath(),
                    depth = list.GetDepth(),
                    childLists = hierarchical ? list.ChildLists.Select(FormatList).ToList() : null
                };
            }

            return new
            {
                success = true,
                lists = lists.Select(FormatList).ToList(),
                totalCount = lists.Count,
                hierarchical = hierarchical
            };
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unexpected error listing all lists");
            return new
            {
                success = false,
                error = "internal_error",
                message = "An unexpected error occurred while listing the lists"
            };
        }
    }
}
