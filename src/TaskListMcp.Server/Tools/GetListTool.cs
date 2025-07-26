using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP Tool for retrieving a specific task list
/// </summary>
[McpServerToolType]
public static class GetListTool
{
    [McpServerTool]
    [Description("Retrieves details of a specific task list by ID")]
    public static async Task<object> GetListAsync(
        [Description("ID of the list to retrieve")] int listId,
        ListService listService = null!,
        ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("Retrieving list {ListId}", listId);

            var list = await listService.GetListByIdAsync(listId);

            if (list == null)
            {
                logger?.LogWarning("List {ListId} not found", listId);
                return new
                {
                    success = false,
                    error = "not_found",
                    message = $"List with ID {listId} not found"
                };
            }

            logger?.LogInformation("Successfully retrieved list {ListId}", listId);

            return new
            {
                success = true,
                list = new
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
                    depth = list.GetDepth()
                }
            };
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unexpected error retrieving list {ListId}", listId);
            return new
            {
                success = false,
                error = "internal_error",
                message = "An unexpected error occurred while retrieving the list"
            };
        }
    }
}
