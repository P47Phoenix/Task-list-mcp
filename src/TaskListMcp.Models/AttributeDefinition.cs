namespace TaskListMcp.Models;

/// <summary>
/// Represents a custom attribute definition
/// </summary>
public class AttributeDefinition
{
    /// <summary>
    /// Unique identifier for the attribute definition
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Name of the attribute
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of the attribute (text, number, date, boolean, etc.)
    /// </summary>
    public AttributeType Type { get; set; }
    
    /// <summary>
    /// Validation rules for the attribute (JSON)
    /// </summary>
    public string? ValidationRules { get; set; }
    
    /// <summary>
    /// Default value for the attribute
    /// </summary>
    public string? DefaultValue { get; set; }
    
    /// <summary>
    /// Whether this attribute is required
    /// </summary>
    public bool IsRequired { get; set; }
    
    /// <summary>
    /// When the attribute definition was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
