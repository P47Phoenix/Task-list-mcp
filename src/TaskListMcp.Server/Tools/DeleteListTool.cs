using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP Tool for deleting task lists
/// </summary>
[McpServerToolType]
public static class DeleteListTool
{
    [McpServerTool]
    [Description("Deletes a task list with optional cascade delete")]
    public static async Task<object> DeleteListAsync(
        [Description("ID of the list to delete")] int listId,
        [Description("Whether to cascade delete child lists and orphan tasks (default: false)")] bool cascadeDelete = false,
        ListService listService = null!,
        ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("Deleting list {ListId} with cascade={CascadeDelete}", listId, cascadeDelete);

            var deleted = await listService.DeleteListAsync(listId, cascadeDelete);

            if (deleted)
            {
                logger?.LogInformation("Successfully deleted list with ID {ListId}", listId);
                return new
                {
                    success = true,
                    message = $"List {listId} deleted successfully"
                };
            }
            else
            {
                logger?.LogWarning("List {ListId} not found for deletion", listId);
                return new
                {
                    success = false,
                    error = "not_found",
                    message = $"List with ID {listId} not found"
                };
            }
        }
        catch (InvalidOperationException ex)
        {
            logger?.LogWarning("Business logic error deleting list {ListId}: {Error}", listId, ex.Message);
            return new
            {
                success = false,
                error = "business_logic_error",
                message = ex.Message
            };
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unexpected error deleting list {ListId}", listId);
            return new
            {
                success = false,
                error = "internal_error",
                message = "An unexpected error occurred while deleting the list"
            };
        }
    }
}
