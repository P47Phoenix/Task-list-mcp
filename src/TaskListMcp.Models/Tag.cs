namespace TaskListMcp.Models;

/// <summary>
/// Represents a tag that can be applied to tasks and lists
/// </summary>
public class Tag
{
    /// <summary>
    /// Unique identifier for the tag
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Name of the tag
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Color associated with the tag (hex color code)
    /// </summary>
    public string? Color { get; set; }
    
    /// <summary>
    /// ID of the parent tag (for hierarchical tags)
    /// </summary>
    public int? ParentId { get; set; }
    
    /// <summary>
    /// Parent tag (for hierarchical structure)
    /// </summary>
    public Tag? Parent { get; set; }
    
    /// <summary>
    /// Child tags
    /// </summary>
    public List<Tag> Children { get; set; } = new();
    
    /// <summary>
    /// When the tag was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Tasks that have this tag
    /// </summary>
    public List<TaskItem> Tasks { get; set; } = new();
    
    /// <summary>
    /// Task lists that have this tag
    /// </summary>
    public List<TaskList> TaskLists { get; set; } = new();
    
    /// <summary>
    /// Number of tasks using this tag
    /// </summary>
    public int TaskCount => Tasks?.Count ?? 0;
    
    /// <summary>
    /// Number of lists using this tag
    /// </summary>
    public int ListCount => TaskLists?.Count ?? 0;
    
    /// <summary>
    /// Gets the full path of the tag (e.g., "parent/child/grandchild")
    /// </summary>
    public string GetPath()
    {
        if (Parent == null)
            return Name;
        return $"{Parent.GetPath()}/{Name}";
    }
    
    /// <summary>
    /// Gets the depth level of the tag in the hierarchy
    /// </summary>
    public int GetDepth()
    {
        if (Parent == null)
            return 0;
        return Parent.GetDepth() + 1;
    }
}
