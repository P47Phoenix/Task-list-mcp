namespace TaskListMcp.Models;

/// <summary>
/// Represents a collection of tasks (task list)
/// </summary>
public class TaskList
{
    /// <summary>
    /// Unique identifier for the task list
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Name of the task list
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the task list
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// ID of the parent list (for nested lists)
    /// </summary>
    public int? ParentListId { get; set; }
    
    /// <summary>
    /// Name of the parent list (read-only from joins)
    /// </summary>
    public string? ParentListName { get; set; }
    
    /// <summary>
    /// Parent task list (for nested structure)
    /// </summary>
    public TaskList? Parent { get; set; }
    
    /// <summary>
    /// Child task lists (sub-lists)
    /// </summary>
    public List<TaskList> ChildLists { get; set; } = new();
    
    /// <summary>
    /// Number of tasks in this list (read-only from queries)
    /// </summary>
    public int TaskCount { get; set; }
    
    /// <summary>
    /// Number of child lists (read-only from queries)
    /// </summary>
    public int ChildListCount { get; set; }
    
    /// <summary>
    /// Tasks contained in this list
    /// </summary>
    public List<TaskItem> Tasks { get; set; } = new();
    
    /// <summary>
    /// When the list was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the list was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the list was soft-deleted (null if not deleted)
    /// </summary>
    public DateTime? DeletedAt { get; set; }
    
    /// <summary>
    /// Tags associated with this list
    /// </summary>
    public List<Tag> Tags { get; set; } = new();
    
    /// <summary>
    /// Custom attributes for this list
    /// </summary>
    public List<ListAttribute> Attributes { get; set; } = new();
    
    /// <summary>
    /// Gets the full hierarchical path of this list
    /// </summary>
    public string GetPath()
    {
        var path = new List<string>();
        var current = this;
        
        while (current != null)
        {
            path.Insert(0, current.Name);
            current = current.Parent;
        }
        
        return string.Join(" > ", path);
    }
    
    /// <summary>
    /// Gets the depth level of this list in the hierarchy
    /// </summary>
    public int GetDepth()
    {
        int depth = 0;
        var current = Parent;
        
        while (current != null)
        {
            depth++;
            current = current.Parent;
        }
        
        return depth;
    }
}
