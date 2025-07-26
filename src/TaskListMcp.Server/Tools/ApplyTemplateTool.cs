using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for applying templates to create new lists
/// </summary>
[McpServerToolType]
public class ApplyTemplateTool
{
    /// <summary>
    /// Applies a template to create a new list with tasks
    /// </summary>
    [McpServerTool(Name = "apply_template")]
    [Description("Applies a template to create a new list with tasks")]
    public static async Task<object> ApplyTemplateAsync(
        [Description("The ID of the template to apply")] int templateId,
        [Description("The name for the new list")] string listName,
        [Description("Optional description for the new list")] string? listDescription = null,
        [Description("Optional parent list ID")] int? parentListId = null,
        TemplateService templateService = null!,
        ILogger<ApplyTemplateTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Applying template {TemplateId} to create list: {ListName}", templateId, listName);

            var newList = await templateService.ApplyTemplateAsync(templateId, listName, listDescription, parentListId);

            if (newList == null)
            {
                logger.LogWarning("Failed to apply template {TemplateId}", templateId);
                return new { success = false, error = $"Failed to apply template {templateId}. Template may not exist." };
            }

            logger.LogInformation("Successfully applied template {TemplateId} to create list: {ListName} with ID: {ListId}", 
                templateId, listName, newList.Id);

            return new
            {
                success = true,
                message = $"Template applied successfully. Created list '{newList.Name}'",
                list = new
                {
                    id = newList.Id,
                    name = newList.Name,
                    description = newList.Description,
                    parentListId = newList.ParentListId,
                    createdAt = newList.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying template {TemplateId}", templateId);
            return new { success = false, error = $"Error applying template: {ex.Message}" };
        }
    }
}
