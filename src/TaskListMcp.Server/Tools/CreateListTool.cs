using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP Tool for creating new task lists
/// </summary>
[McpServerToolType]
public static class CreateListTool
{
    [McpServerTool]
    [Description("Creates a new task list with optional parent hierarchy")]
    public static async Task<object> CreateListAsync(
        [Description("Name of the new list (required, max 200 characters)")] string name,
        [Description("Optional description of the list")] string? description = null,
        [Description("Optional parent list ID for nested lists")] int? parentListId = null,
        ListService listService = null!,
        ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("Creating list: {Name}, Parent: {ParentListId}", name, parentListId);

            var list = await listService.CreateListAsync(name, description, parentListId);

            logger?.LogInformation("Successfully created list with ID {ListId}", list.Id);

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
                message = $"List '{name}' created successfully"
            };
        }
        catch (ArgumentException ex)
        {
            logger?.LogWarning("Validation error creating list: {Error}", ex.Message);
            return new
            {
                success = false,
                error = "validation_error",
                message = ex.Message
            };
        }
        catch (InvalidOperationException ex)
        {
            logger?.LogWarning("Business logic error creating list: {Error}", ex.Message);
            return new
            {
                success = false,
                error = "business_logic_error",
                message = ex.Message
            };
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unexpected error creating list");
            return new
            {
                success = false,
                error = "internal_error",
                message = "An unexpected error occurred while creating the list"
            };
        }
    }
}
