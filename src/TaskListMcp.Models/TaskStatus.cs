namespace TaskListMcp.Models;

/// <summary>
/// Represents the possible states of a task
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// Task has been created but not yet started
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Task is currently being worked on
    /// </summary>
    InProgress = 1,
    
    /// <summary>
    /// Task has been completed successfully
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// Task has been cancelled or abandoned
    /// </summary>
    Cancelled = 3,
    
    /// <summary>
    /// Task is waiting on external dependency
    /// </summary>
    Blocked = 4
}
