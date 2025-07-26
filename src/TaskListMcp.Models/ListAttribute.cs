namespace TaskListMcp.Models;

/// <summary>
/// Represents a custom attribute value for a task list
/// </summary>
public class ListAttribute
{
    /// <summary>
    /// ID of the task list this attribute belongs to
    /// </summary>
    public int TaskListId { get; set; }
    
    /// <summary>
    /// The task list this attribute belongs to
    /// </summary>
    public TaskList? TaskList { get; set; }
    
    /// <summary>
    /// ID of the attribute definition
    /// </summary>
    public int AttributeDefinitionId { get; set; }
    
    /// <summary>
    /// The attribute definition
    /// </summary>
    public AttributeDefinition? AttributeDefinition { get; set; }
    
    /// <summary>
    /// The value of the attribute (stored as string, parsed based on type)
    /// </summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// When this attribute value was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this attribute value was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
