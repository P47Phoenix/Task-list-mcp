using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for getting template details
/// </summary>
[McpServerToolType]
public class GetTemplateTool
{
    /// <summary>
    /// Gets a template by ID with all its tasks
    /// </summary>
    [McpServerTool(Name = "get_template")]
    [Description("Gets a template by ID with all its tasks")]
    public static async Task<object> GetTemplateAsync(
        [Description("The ID of the template to retrieve")] int templateId,
        TemplateService templateService = null!,
        ILogger<GetTemplateTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Retrieving template with ID: {TemplateId}", templateId);

            var template = await templateService.GetTemplateByIdAsync(templateId);

            if (template == null)
            {
                logger.LogWarning("Template with ID {TemplateId} not found", templateId);
                return new { success = false, error = $"Template with ID {templateId} not found" };
            }

            logger.LogInformation("Successfully retrieved template: {TemplateName}", template.Name);

            return new
            {
                success = true,
                template = new
                {
                    id = template.Id,
                    name = template.Name,
                    description = template.Description,
                    category = template.Category,
                    version = template.Version,
                    taskCount = template.TaskCount,
                    createdAt = template.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    updatedAt = template.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    deletedAt = template.DeletedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                    tasks = template.Tasks.OrderBy(t => t.OrderIndex).Select(t => new
                    {
                        id = t.Id,
                        title = t.Title,
                        description = t.Description,
                        orderIndex = t.OrderIndex,
                        estimatedHours = t.EstimatedHours,
                        priority = t.Priority,
                        createdAt = t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                    }).ToList()
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving template with ID: {TemplateId}", templateId);
            return new { success = false, error = $"Error retrieving template: {ex.Message}" };
        }
    }
}
