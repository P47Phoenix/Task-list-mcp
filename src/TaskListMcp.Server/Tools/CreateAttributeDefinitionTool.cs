using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for creating attribute definitions
/// </summary>
[McpServerToolType]
public class CreateAttributeDefinitionTool
{
    /// <summary>
    /// Create a new attribute definition
    /// </summary>
    [McpServerTool(Name = "create_attribute_definition")]
    [Description("Create a new custom attribute definition that can be used for tasks and lists")]
    public static async Task<object> CreateAttributeDefinitionAsync(
        [Description("Name of the attribute")] string name,
        [Description("Type of the attribute (Text, Integer, Decimal, Date, DateTime, Boolean, SingleChoice, MultipleChoice, Url, FileReference)")] string type,
        [Description("Whether this attribute is required (default: false)")] bool isRequired = false,
        [Description("Default value for the attribute (optional)")] string? defaultValue = null,
        [Description("JSON validation rules for the attribute (optional)")] string? validationRules = null,
        AttributeService attributeService = null!,
        ILogger<CreateAttributeDefinitionTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Creating attribute definition: {Name} (Type: {Type})", name, type);

            // Parse the attribute type
            if (!Enum.TryParse<AttributeType>(type, true, out var attributeType))
            {
                var validTypes = string.Join(", ", Enum.GetNames<AttributeType>());
                throw new ArgumentException($"Invalid attribute type '{type}'. Valid types are: {validTypes}");
            }

            // Check if attribute with this name already exists
            var existing = await attributeService.GetAttributeDefinitionByNameAsync(name);
            if (existing != null)
            {
                throw new ArgumentException($"Attribute definition with name '{name}' already exists");
            }

            var attributeDefinition = await attributeService.CreateAttributeDefinitionAsync(
                name, 
                attributeType, 
                isRequired, 
                defaultValue, 
                validationRules);

            return new
            {
                success = true,
                message = $"Created attribute definition '{name}' with ID {attributeDefinition.Id}",
                attributeDefinition = new
                {
                    id = attributeDefinition.Id,
                    name = attributeDefinition.Name,
                    type = attributeDefinition.Type.ToString(),
                    isRequired = attributeDefinition.IsRequired,
                    defaultValue = attributeDefinition.DefaultValue,
                    validationRules = attributeDefinition.ValidationRules,
                    createdAt = attributeDefinition.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating attribute definition");
            return new
            {
                success = false,
                error = ex.Message
            };
        }
    }
}
