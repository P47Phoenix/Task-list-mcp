namespace TaskListMcp.Models;

/// <summary>
/// Search and filter criteria for tasks and lists
/// </summary>
public class SearchFilter
{
    /// <summary>
    /// Full-text search query
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Filter by task status
    /// </summary>
    public Models.TaskStatus? Status { get; set; }

    /// <summary>
    /// Filter by priority level
    /// </summary>
    public Priority? Priority { get; set; }

    /// <summary>
    /// Filter by specific list ID
    /// </summary>
    public int? ListId { get; set; }

    /// <summary>
    /// Filter by tag names
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Filter by due date range
    /// </summary>
    public DateTime? DueDateFrom { get; set; }

    /// <summary>
    /// Filter by due date range
    /// </summary>
    public DateTime? DueDateTo { get; set; }

    /// <summary>
    /// Filter by creation date range
    /// </summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// Filter by creation date range
    /// </summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>
    /// Filter by completion date range
    /// </summary>
    public DateTime? CompletedFrom { get; set; }

    /// <summary>
    /// Filter by completion date range
    /// </summary>
    public DateTime? CompletedTo { get; set; }

    /// <summary>
    /// Filter by custom attributes (attribute name -> value)
    /// </summary>
    public Dictionary<string, string>? Attributes { get; set; }

    /// <summary>
    /// Include completed tasks in results (default: true)
    /// </summary>
    public bool IncludeCompleted { get; set; } = true;

    /// <summary>
    /// Include cancelled tasks in results (default: false)
    /// </summary>
    public bool IncludeCancelled { get; set; } = false;

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Sort order for results
    /// </summary>
    public SearchSortOrder SortOrder { get; set; } = SearchSortOrder.Relevance;

    /// <summary>
    /// Whether to sort in descending order
    /// </summary>
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Sort options for search results
/// </summary>
public enum SearchSortOrder
{
    /// <summary>
    /// Sort by search relevance
    /// </summary>
    Relevance,

    /// <summary>
    /// Sort by creation date
    /// </summary>
    CreatedDate,

    /// <summary>
    /// Sort by due date
    /// </summary>
    DueDate,

    /// <summary>
    /// Sort by priority
    /// </summary>
    Priority,

    /// <summary>
    /// Sort by title alphabetically
    /// </summary>
    Title,

    /// <summary>
    /// Sort by last updated date
    /// </summary>
    UpdatedDate
}
