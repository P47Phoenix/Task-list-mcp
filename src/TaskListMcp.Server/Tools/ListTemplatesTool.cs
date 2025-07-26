using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for listing templates
/// </summary>
[McpServerToolType]
public class ListTemplesTool
{
    /// <summary>
    /// Lists all available templates with optional category filtering
    /// </summary>
    [McpServerTool(Name = "list_templates")]
    [Description("Lists all available templates with optional category filtering")]
    public static async Task<object> ListTemplatesAsync(
        [Description("Optional category to filter templates by")] string? category = null,
        TemplateService templateService = null!,
        ILogger<ListTemplesTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Listing templates with category filter: {Category}", category ?? "none");

            var templates = await templateService.GetAllTemplatesAsync(category);

            logger.LogInformation("Found {TemplateCount} templates", templates.Count);

            return new
            {
                success = true,
                count = templates.Count,
                category = category,
                templates = templates.Select(t => new
                {
                    id = t.Id,
                    name = t.Name,
                    description = t.Description,
                    category = t.Category,
                    version = t.Version,
                    taskCount = t.TaskCount,
                    createdAt = t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    updatedAt = t.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing templates");
            return new { success = false, error = $"Error listing templates: {ex.Message}" };
        }
    }
}
