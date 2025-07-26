using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for deleting tags
/// </summary>
[McpServerToolType]
public class DeleteTagTool
{
    /// <summary>
    /// Deletes a tag and removes all associations
    /// </summary>
    [McpServerTool(Name = "delete_tag")]
    [Description("Deletes a tag and removes all associations with tasks and lists")]
    public static async Task<object> DeleteTagAsync(
        [Description("The ID of the tag to delete")] int tagId,
        TagService tagService = null!,
        ILogger<DeleteTagTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Attempting to delete tag with ID: {TagId}", tagId);

            // Get tag details first for response
            var existingTag = await tagService.GetTagByIdAsync(tagId);
            if (existingTag == null)
            {
                logger.LogWarning("Tag with ID {TagId} not found", tagId);
                return new { success = false, error = $"Tag with ID {tagId} not found" };
            }

            // Delete the tag
            var success = await tagService.DeleteTagAsync(tagId);

            if (success)
            {
                logger.LogInformation("Successfully deleted tag with ID: {TagId}", tagId);
                return new
                {
                    success = true,
                    message = $"Tag '{existingTag.Name}' has been deleted",
                    tagId = tagId
                };
            }
            else
            {
                logger.LogError("Failed to delete tag with ID: {TagId}", tagId);
                return new { success = false, error = "Failed to delete tag" };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting tag with ID: {TagId}", tagId);
            return new { success = false, error = $"Error deleting tag: {ex.Message}" };
        }
    }
}
