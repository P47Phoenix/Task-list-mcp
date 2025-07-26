namespace TaskListMcp.Models;

/// <summary>
/// Represents a task template within a template
/// </summary>
public class TemplateTask
{
    /// <summary>
    /// Unique identifier for the template task
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// ID of the template this task belongs to
    /// </summary>
    public int TemplateId { get; set; }
    
    /// <summary>
    /// Title of the template task
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description of the template task
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Order index for sorting template tasks
    /// </summary>
    public int OrderIndex { get; set; }
    
    /// <summary>
    /// Estimated hours to complete this task
    /// </summary>
    public decimal? EstimatedHours { get; set; }
    
    /// <summary>
    /// Priority level
    /// </summary>
    public Priority Priority { get; set; } = Priority.Normal;
    
    /// <summary>
    /// When the template task was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Reference to the parent template
    /// </summary>
    public Template? Template { get; set; }
}
