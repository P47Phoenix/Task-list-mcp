using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for searching tasks with advanced filtering
/// </summary>
[McpServerToolType]
public class SearchTasksTool
{
    /// <summary>
    /// Search tasks with full-text search and comprehensive filtering options
    /// </summary>
    [McpServerTool(Name = "search_tasks")]
    [Description("Search tasks with full-text search and comprehensive filtering options")]
    public static async Task<object> SearchTasksAsync(
        [Description("Search query for title, description, and notes")] string? query = null,
        [Description("Filter by status (pending, in_progress, completed, cancelled, blocked)")] string? status = null,
        [Description("Filter by priority (low, medium, high, critical)")] string? priority = null,
        [Description("Filter by specific list ID")] int? listId = null,
        [Description("Comma-separated list of tag names to filter by")] string? tags = null,
        [Description("Filter by due date from (YYYY-MM-DD format)")] string? dueDateFrom = null,
        [Description("Filter by due date to (YYYY-MM-DD format)")] string? dueDateTo = null,
        [Description("Include completed tasks (default: true)")] bool includeCompleted = true,
        [Description("Include cancelled tasks (default: false)")] bool includeCancelled = false,
        [Description("Maximum number of results (default: 50)")] int limit = 50,
        [Description("Sort order (relevance, created_date, due_date, priority, title, updated_date)")] string sortOrder = "relevance",
        [Description("Sort in descending order (default: true)")] bool sortDescending = true,
        SearchService searchService = null!,
        ILogger<SearchTasksTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Searching tasks: Query={Query}, Status={Status}, Priority={Priority}", 
                query, status, priority);

            var filter = new SearchFilter
            {
                Query = query,
                IncludeCompleted = includeCompleted,
                IncludeCancelled = includeCancelled,
                Limit = limit,
                SortDescending = sortDescending
            };

            // Parse status
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<Models.TaskStatus>(status, true, out var taskStatus))
            {
                filter.Status = taskStatus;
            }

            // Parse priority
            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<Priority>(priority, true, out var taskPriority))
            {
                filter.Priority = taskPriority;
            }

            // Parse list ID
            if (listId.HasValue)
            {
                filter.ListId = listId.Value;
            }

            // Parse tags
            if (!string.IsNullOrEmpty(tags))
            {
                filter.Tags = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(t => t.Trim())
                                 .ToList();
            }

            // Parse dates
            if (!string.IsNullOrEmpty(dueDateFrom) && DateTime.TryParse(dueDateFrom, out var fromDate))
            {
                filter.DueDateFrom = fromDate;
            }

            if (!string.IsNullOrEmpty(dueDateTo) && DateTime.TryParse(dueDateTo, out var toDate))
            {
                filter.DueDateTo = toDate;
            }

            // Parse sort order
            if (Enum.TryParse<SearchSortOrder>(sortOrder.Replace("_", ""), true, out var sort))
            {
                filter.SortOrder = sort;
            }

            var tasks = await searchService.SearchTasksAsync(filter);

            return new
            {
                success = true,
                message = $"Found {tasks.Count} tasks matching search criteria",
                totalResults = tasks.Count,
                searchCriteria = new
                {
                    query = query,
                    status = status,
                    priority = priority,
                    listId = listId,
                    tags = tags,
                    dueDateFrom = dueDateFrom,
                    dueDateTo = dueDateTo,
                    includeCompleted = includeCompleted,
                    includeCancelled = includeCancelled,
                    limit = limit,
                    sortOrder = sortOrder,
                    sortDescending = sortDescending
                },
                tasks = tasks.Select(t => new
                {
                    id = t.Id,
                    title = t.Title,
                    description = t.Description,
                    status = t.Status.ToString(),
                    priority = t.Priority.ToString(),
                    listId = t.ListId,
                    dueDate = t.DueDate,
                    createdAt = t.CreatedAt,
                    updatedAt = t.UpdatedAt,
                    estimatedHours = t.EstimatedHours
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching tasks");
            return new
            {
                success = false,
                error = ex.Message
            };
        }
    }
}
