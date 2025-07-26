namespace TaskListMcp.Models;

/// <summary>
/// Task priority levels
/// </summary>
public enum Priority
{
    /// <summary>
    /// Normal priority (default)
    /// </summary>
    Normal = 0,
    
    /// <summary>
    /// Low priority
    /// </summary>
    Low = 1,
    
    /// <summary>
    /// High priority
    /// </summary>
    High = 2,
    
    /// <summary>
    /// Critical priority
    /// </summary>
    Critical = 3
}
