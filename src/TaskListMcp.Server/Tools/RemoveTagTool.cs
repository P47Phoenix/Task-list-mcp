using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for removing tags from tasks and lists
/// </summary>
[McpServerToolType]
public class RemoveTagTool
{
    /// <summary>
    /// Removes a tag from a task or list
    /// </summary>
    [McpServerTool(Name = "remove_tag")]
    [Description("Removes a tag from a task or list. Specify either taskId or listId, not both.")]
    public static async Task<object> RemoveTagAsync(
        [Description("The ID of the tag to remove")] int tagId,
        [Description("The ID of the task to remove tag from (use either taskId or listId)")] int? taskId = null,
        [Description("The ID of the list to remove tag from (use either taskId or listId)")] int? listId = null,
        TagService tagService = null!,
        ILogger<RemoveTagTool> logger = null!)
    {
        try
        {
            // Validate parameters
            if (taskId.HasValue && listId.HasValue)
            {
                return new { success = false, error = "Cannot specify both taskId and listId. Choose one." };
            }

            if (!taskId.HasValue && !listId.HasValue)
            {
                return new { success = false, error = "Must specify either taskId or listId." };
            }

            bool success;
            string targetType;
            int targetId;

            if (taskId.HasValue)
            {
                targetType = "task";
                targetId = taskId.Value;
                logger.LogInformation("Removing tag {TagId} from task {TaskId}", tagId, taskId.Value);
                success = await tagService.RemoveTagFromTaskAsync(taskId.Value, tagId);
            }
            else
            {
                targetType = "list";
                targetId = listId!.Value;
                logger.LogInformation("Removing tag {TagId} from list {ListId}", tagId, listId.Value);
                success = await tagService.RemoveTagFromListAsync(listId.Value, tagId);
            }

            if (success)
            {
                logger.LogInformation("Successfully removed tag {TagId} from {TargetType} {TargetId}", tagId, targetType, targetId);
                return new
                {
                    success = true,
                    message = $"Tag {tagId} removed from {targetType} {targetId} successfully",
                    tagId = tagId,
                    targetType = targetType,
                    targetId = targetId
                };
            }
            else
            {
                logger.LogWarning("Tag {TagId} was not associated with {TargetType} {TargetId}", tagId, targetType, targetId);
                return new { success = false, error = $"Tag was not associated with the {targetType}." };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing tag {TagId}", tagId);
            return new { success = false, error = $"Error removing tag: {ex.Message}" };
        }
    }
}
