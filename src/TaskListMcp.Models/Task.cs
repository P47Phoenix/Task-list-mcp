namespace TaskListMcp.Models;

/// <summary>
/// Represents a task in the task management system
/// </summary>
public class TaskItem
{
    /// <summary>
    /// Unique identifier for the task
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Title of the task
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description of the task
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Current status of the task
    /// </summary>
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    
    /// <summary>
    /// ID of the task list this task belongs to
    /// </summary>
    public int ListId { get; set; }
    
    /// <summary>
    /// Name of the task list this task belongs to (read-only from joins)
    /// </summary>
    public string? ListName { get; set; }
    
    /// <summary>
    /// The task list this task belongs to
    /// </summary>
    public TaskList? List { get; set; }
    
    /// <summary>
    /// When the task was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the task was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the task was soft-deleted (null if not deleted)
    /// </summary>
    public DateTime? DeletedAt { get; set; }
    
    /// <summary>
    /// Due date for the task
    /// </summary>
    public DateTime? DueDate { get; set; }
    
    /// <summary>
    /// Priority level of the task
    /// </summary>
    public Priority Priority { get; set; } = Priority.Normal;
    
    /// <summary>
    /// Estimated effort in hours
    /// </summary>
    public decimal? EstimatedHours { get; set; }
    
    /// <summary>
    /// Tags associated with this task
    /// </summary>
    public List<Tag> Tags { get; set; } = new();
    
    /// <summary>
    /// Custom attributes for this task
    /// </summary>
    public List<TaskAttribute> Attributes { get; set; } = new();
}
