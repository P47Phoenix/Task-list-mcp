using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for deleting templates
/// </summary>
[McpServerToolType]
public class DeleteTemplateTool
{
    /// <summary>
    /// Deletes a template by ID (soft delete)
    /// </summary>
    [McpServerTool(Name = "delete_template")]
    [Description("Deletes a template by ID (soft delete)")]
    public static async Task<object> DeleteTemplateAsync(
        [Description("The ID of the template to delete")] int templateId,
        TemplateService templateService = null!,
        ILogger<DeleteTemplateTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Attempting to delete template with ID: {TemplateId}", templateId);

            // Check if template exists first
            var existingTemplate = await templateService.GetTemplateByIdAsync(templateId);
            if (existingTemplate == null)
            {
                logger.LogWarning("Template with ID {TemplateId} not found", templateId);
                return new { success = false, error = $"Template with ID {templateId} not found" };
            }

            // Delete the template
            var success = await templateService.DeleteTemplateAsync(templateId);

            if (success)
            {
                logger.LogInformation("Successfully deleted template with ID: {TemplateId}", templateId);
                return new
                {
                    success = true,
                    message = $"Template '{existingTemplate.Name}' has been deleted",
                    templateId = templateId
                };
            }
            else
            {
                logger.LogError("Failed to delete template with ID: {TemplateId}", templateId);
                return new { success = false, error = "Failed to delete template" };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting template with ID: {TemplateId}", templateId);
            return new { success = false, error = $"Error deleting template: {ex.Message}" };
        }
    }
}
