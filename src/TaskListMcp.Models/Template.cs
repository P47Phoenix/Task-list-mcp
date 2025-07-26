namespace TaskListMcp.Models;

/// <summary>
/// Represents a task list template that can be used to create new lists with predefined tasks
/// </summary>
public class Template
{
    /// <summary>
    /// Unique identifier for the template
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Name of the template
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description of the template
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Category for organizing templates
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Version of the template
    /// </summary>
    public string Version { get; set; } = "1.0";
    
    /// <summary>
    /// When the template was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the template was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the template was deleted (null if not deleted)
    /// </summary>
    public DateTime? DeletedAt { get; set; }
    
    /// <summary>
    /// Collection of template tasks
    /// </summary>
    public List<TemplateTask> Tasks { get; set; } = new List<TemplateTask>();
    
    /// <summary>
    /// Number of tasks in this template
    /// </summary>
    public int TaskCount => Tasks.Count;
}
