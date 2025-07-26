using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for creating templates
/// </summary>
[McpServerToolType]
public class CreateTemplateTool
{
    /// <summary>
    /// Creates a new template, optionally from an existing list
    /// </summary>
    [McpServerTool(Name = "create_template")]
    [Description("Creates a new template, optionally from an existing list")]
    public static async Task<object> CreateTemplateAsync(
        [Description("The name of the template")] string name,
        [Description("Optional description of the template")] string? description = null,
        [Description("Optional category for organizing templates")] string? category = null,
        [Description("Optional list ID to create template from (extracts tasks from this list)")] int? fromListId = null,
        TemplateService templateService = null!,
        ILogger<CreateTemplateTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Creating template: {TemplateName}", name);

            Template? template;
            
            if (fromListId.HasValue)
            {
                // Create template from existing list
                template = await templateService.CreateTemplateFromListAsync(fromListId.Value, name, description, category);
            }
            else
            {
                // Create empty template
                template = await templateService.CreateTemplateAsync(name, description, category);
            }

            if (template == null)
            {
                logger.LogWarning("Failed to create template: {TemplateName}", name);
                return new { success = false, error = "Failed to create template" };
            }

            logger.LogInformation("Successfully created template: {TemplateName} with ID: {TemplateId}", name, template.Id);

            return new
            {
                success = true,
                message = $"Template '{template.Name}' created successfully",
                template = new
                {
                    id = template.Id,
                    name = template.Name,
                    description = template.Description,
                    category = template.Category,
                    version = template.Version,
                    taskCount = template.TaskCount,
                    createdAt = template.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating template: {TemplateName}", name);
            return new { success = false, error = $"Error creating template: {ex.Message}" };
        }
    }
}
