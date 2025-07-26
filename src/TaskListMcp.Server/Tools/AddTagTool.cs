using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for adding tags to tasks and lists
/// </summary>
[McpServerToolType]
public class AddTagTool
{
    /// <summary>
    /// Adds a tag to a task or list
    /// </summary>
    [McpServerTool(Name = "add_tag")]
    [Description("Adds a tag to a task or list. Specify either taskId or listId, not both.")]
    public static async Task<object> AddTagAsync(
        [Description("The ID of the tag to add")] int tagId,
        [Description("The ID of the task to tag (use either taskId or listId)")] int? taskId = null,
        [Description("The ID of the list to tag (use either taskId or listId)")] int? listId = null,
        TagService tagService = null!,
        ILogger<AddTagTool> logger = null!)
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
                logger.LogInformation("Adding tag {TagId} to task {TaskId}", tagId, taskId.Value);
                success = await tagService.AddTagToTaskAsync(taskId.Value, tagId);
            }
            else
            {
                targetType = "list";
                targetId = listId!.Value;
                logger.LogInformation("Adding tag {TagId} to list {ListId}", tagId, listId.Value);
                success = await tagService.AddTagToListAsync(listId.Value, tagId);
            }

            if (success)
            {
                logger.LogInformation("Successfully added tag {TagId} to {TargetType} {TargetId}", tagId, targetType, targetId);
                return new
                {
                    success = true,
                    message = $"Tag {tagId} added to {targetType} {targetId} successfully",
                    tagId = tagId,
                    targetType = targetType,
                    targetId = targetId
                };
            }
            else
            {
                logger.LogWarning("Failed to add tag {TagId} to {TargetType} {TargetId}", tagId, targetType, targetId);
                return new { success = false, error = $"Failed to add tag. {targetType} or tag may not exist." };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding tag {TagId}", tagId);
            return new { success = false, error = $"Error adding tag: {ex.Message}" };
        }
    }
}
