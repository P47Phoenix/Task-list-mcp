using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP Tool for updating existing task lists
/// </summary>
[McpServerToolType]
public static class UpdateListTool
{
    [McpServerTool]
    [Description("Updates an existing task list's properties")]
    public static async Task<object> UpdateListAsync(
        [Description("ID of the list to update")] int listId,
        [Description("New name for the list (max 200 characters)")] string? name = null,
        [Description("New description for the list")] string? description = null,
        [Description("New parent list ID (null to make it a root list)")] int? parentListId = null,
        ListService listService = null!,
        ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("Updating list {ListId}: Name={Name}, Parent={ParentListId}", 
                listId, name, parentListId);

            var list = await listService.UpdateListAsync(listId, name, description, parentListId);

            logger?.LogInformation("Successfully updated list with ID {ListId}", list.Id);

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
                    updatedAt = list.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC")
                },
                message = $"List '{list.Name}' updated successfully"
            };
        }
        catch (ArgumentException ex)
        {
            logger?.LogWarning("Validation error updating list {ListId}: {Error}", listId, ex.Message);
            return new
            {
                success = false,
                error = "validation_error",
                message = ex.Message
            };
        }
        catch (InvalidOperationException ex)
        {
            logger?.LogWarning("Business logic error updating list {ListId}: {Error}", listId, ex.Message);
            return new
            {
                success = false,
                error = "business_logic_error",
                message = ex.Message
            };
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unexpected error updating list {ListId}", listId);
            return new
            {
                success = false,
                error = "internal_error",
                message = "An unexpected error occurred while updating the list"
            };
        }
    }
}
