using Microsoft.Extensions.Logging;
using TaskListMcp.Data;
using TaskListMcp.Models;
using Microsoft.Data.Sqlite;
using System.Text;

namespace TaskListMcp.Core.Services;

/// <summary>
/// Service for searching and filtering tasks and lists
/// </summary>
public class SearchService
{
    private readonly DatabaseManager _databaseManager;
    private readonly ILogger<SearchService> _logger;

    public SearchService(DatabaseManager databaseManager, ILogger<SearchService> logger)
    {
        _databaseManager = databaseManager ?? throw new ArgumentNullException(nameof(databaseManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Search tasks with comprehensive filtering
    /// </summary>
    public async Task<List<TaskItem>> SearchTasksAsync(SearchFilter filter)
    {
        _logger.LogInformation("Searching tasks with filter: Query={Query}, Status={Status}, Priority={Priority}", 
            filter.Query, filter.Status, filter.Priority);

        using var connection = await _databaseManager.GetConnectionAsync();

        var (sql, parameters) = BuildTaskSearchQuery(filter);
        
        using var command = new SqliteCommand(sql, connection);
        foreach (var param in parameters)
        {
            command.Parameters.Add(param);
        }

        using var reader = await command.ExecuteReaderAsync();
        var tasks = new List<TaskItem>();

        while (await reader.ReadAsync())
        {
            tasks.Add(new TaskItem
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                Status = (Models.TaskStatus)reader.GetInt32(reader.GetOrdinal("status")),
                Priority = (Priority)reader.GetInt32(reader.GetOrdinal("priority")),
                ListId = reader.GetInt32(reader.GetOrdinal("list_id")),
                DueDate = reader.IsDBNull(reader.GetOrdinal("due_date")) ? null : reader.GetDateTime(reader.GetOrdinal("due_date")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                EstimatedHours = reader.IsDBNull(reader.GetOrdinal("estimated_hours")) ? null : Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("estimated_hours")))
            });
        }

        _logger.LogInformation("Found {Count} tasks matching search criteria", tasks.Count);
        return tasks;
    }

    /// <summary>
    /// Search task lists with filtering
    /// </summary>
    public async Task<List<TaskList>> SearchListsAsync(SearchFilter filter)
    {
        _logger.LogInformation("Searching lists with filter: Query={Query}", filter.Query);

        using var connection = await _databaseManager.GetConnectionAsync();

        var (sql, parameters) = BuildListSearchQuery(filter);
        
        using var command = new SqliteCommand(sql, connection);
        foreach (var param in parameters)
        {
            command.Parameters.Add(param);
        }

        using var reader = await command.ExecuteReaderAsync();
        var lists = new List<TaskList>();

        while (await reader.ReadAsync())
        {
            lists.Add(new TaskList
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                ParentListId = reader.IsDBNull(reader.GetOrdinal("parent_list_id")) ? null : reader.GetInt32(reader.GetOrdinal("parent_list_id")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            });
        }

        _logger.LogInformation("Found {Count} lists matching search criteria", lists.Count);
        return lists;
    }

    /// <summary>
    /// Get search suggestions based on partial query
    /// </summary>
    public async Task<List<string>> GetSearchSuggestionsAsync(string partialQuery, int maxSuggestions = 10)
    {
        _logger.LogDebug("Getting search suggestions for: {Query}", partialQuery);

        if (string.IsNullOrWhiteSpace(partialQuery) || partialQuery.Length < 2)
        {
            return new List<string>();
        }

        using var connection = await _databaseManager.GetConnectionAsync();

        var sql = @"
            SELECT DISTINCT title as suggestion FROM tasks 
            WHERE title LIKE @query 
            UNION
            SELECT DISTINCT name as suggestion FROM task_lists 
            WHERE name LIKE @query 
            UNION
            SELECT DISTINCT name as suggestion FROM tags 
            WHERE name LIKE @query
            ORDER BY suggestion
            LIMIT @limit";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@query", $"%{partialQuery}%");
        command.Parameters.AddWithValue("@limit", maxSuggestions);

        using var reader = await command.ExecuteReaderAsync();
        var suggestions = new List<string>();

        while (await reader.ReadAsync())
        {
            suggestions.Add(reader.GetString(reader.GetOrdinal("suggestion")));
        }

        _logger.LogDebug("Found {Count} search suggestions", suggestions.Count);
        return suggestions;
    }

    /// <summary>
    /// Get task count by status for analytics
    /// </summary>
    public async Task<Dictionary<Models.TaskStatus, int>> GetTaskCountByStatusAsync(int? listId = null)
    {
        _logger.LogDebug("Getting task count by status for list: {ListId}", listId);

        using var connection = await _databaseManager.GetConnectionAsync();

        var sql = @"
            SELECT status, COUNT(*) as count 
            FROM tasks 
            WHERE (@listId IS NULL OR list_id = @listId)
            GROUP BY status";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@listId", (object?)listId ?? DBNull.Value);

        using var reader = await command.ExecuteReaderAsync();
        var counts = new Dictionary<Models.TaskStatus, int>();

        while (await reader.ReadAsync())
        {
            var status = (Models.TaskStatus)reader.GetInt32(reader.GetOrdinal("status"));
            var count = reader.GetInt32(reader.GetOrdinal("count"));
            counts[status] = count;
        }

        return counts;
    }

    /// <summary>
    /// Get most used tags
    /// </summary>
    public async Task<List<(string TagName, int UsageCount)>> GetMostUsedTagsAsync(int maxResults = 20)
    {
        _logger.LogDebug("Getting most used tags (max: {MaxResults})", maxResults);

        using var connection = await _databaseManager.GetConnectionAsync();

        var sql = @"
            SELECT t.name, 
                   (SELECT COUNT(*) FROM task_tags tt WHERE tt.tag_id = t.id) + 
                   (SELECT COUNT(*) FROM list_tags lt WHERE lt.tag_id = t.id) as usage_count
            FROM tags t
            ORDER BY usage_count DESC
            LIMIT @maxResults";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@maxResults", maxResults);

        using var reader = await command.ExecuteReaderAsync();
        var tags = new List<(string TagName, int UsageCount)>();

        while (await reader.ReadAsync())
        {
            var tagName = reader.GetString(reader.GetOrdinal("name"));
            var usageCount = reader.GetInt32(reader.GetOrdinal("usage_count"));
            tags.Add((tagName, usageCount));
        }

        _logger.LogDebug("Found {Count} most used tags", tags.Count);
        return tags;
    }

    #region Private Helper Methods

    private (string sql, List<SqliteParameter> parameters) BuildTaskSearchQuery(SearchFilter filter)
    {
        var sql = new StringBuilder(@"
            SELECT DISTINCT t.id, t.title, t.description, t.status, t.priority, t.list_id, 
                   t.due_date, t.created_at, t.updated_at, t.estimated_hours
            FROM tasks t");

        var parameters = new List<SqliteParameter>();
        var whereConditions = new List<string>();
        var joins = new List<string>();

        // Text search
        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            whereConditions.Add("(t.title LIKE @query OR t.description LIKE @query OR t.notes LIKE @query)");
            parameters.Add(new SqliteParameter("@query", $"%{filter.Query}%"));
        }

        // Status filter
        if (filter.Status.HasValue)
        {
            whereConditions.Add("t.status = @status");
            parameters.Add(new SqliteParameter("@status", (int)filter.Status.Value));
        }

        // Priority filter
        if (filter.Priority.HasValue)
        {
            whereConditions.Add("t.priority = @priority");
            parameters.Add(new SqliteParameter("@priority", (int)filter.Priority.Value));
        }

        // List filter
        if (filter.ListId.HasValue)
        {
            whereConditions.Add("t.list_id = @listId");
            parameters.Add(new SqliteParameter("@listId", filter.ListId.Value));
        }

        // Tag filter
        if (filter.Tags != null && filter.Tags.Any())
        {
            joins.Add("LEFT JOIN task_tags tt ON t.id = tt.task_id");
            joins.Add("LEFT JOIN tags tag ON tt.tag_id = tag.id");
            
            var tagConditions = filter.Tags.Select((tag, index) =>
            {
                var paramName = $"@tag{index}";
                parameters.Add(new SqliteParameter(paramName, tag));
                return $"tag.name = {paramName}";
            });
            
            whereConditions.Add($"({string.Join(" OR ", tagConditions)})");
        }

        // Date filters
        if (filter.DueDateFrom.HasValue)
        {
            whereConditions.Add("t.due_date >= @dueDateFrom");
            parameters.Add(new SqliteParameter("@dueDateFrom", filter.DueDateFrom.Value));
        }

        if (filter.DueDateTo.HasValue)
        {
            whereConditions.Add("t.due_date <= @dueDateTo");
            parameters.Add(new SqliteParameter("@dueDateTo", filter.DueDateTo.Value));
        }

        if (filter.CreatedFrom.HasValue)
        {
            whereConditions.Add("t.created_at >= @createdFrom");
            parameters.Add(new SqliteParameter("@createdFrom", filter.CreatedFrom.Value));
        }

        if (filter.CreatedTo.HasValue)
        {
            whereConditions.Add("t.created_at <= @createdTo");
            parameters.Add(new SqliteParameter("@createdTo", filter.CreatedTo.Value));
        }

        if (filter.CompletedFrom.HasValue)
        {
            whereConditions.Add("t.completed_at >= @completedFrom");
            parameters.Add(new SqliteParameter("@completedFrom", filter.CompletedFrom.Value));
        }

        if (filter.CompletedTo.HasValue)
        {
            whereConditions.Add("t.completed_at <= @completedTo");
            parameters.Add(new SqliteParameter("@completedTo", filter.CompletedTo.Value));
        }

        // Include/exclude completed and cancelled
        if (!filter.IncludeCompleted)
        {
            whereConditions.Add("t.status != @completedStatus");
            parameters.Add(new SqliteParameter("@completedStatus", (int)Models.TaskStatus.Completed));
        }

        if (!filter.IncludeCancelled)
        {
            whereConditions.Add("t.status != @cancelledStatus");
            parameters.Add(new SqliteParameter("@cancelledStatus", (int)Models.TaskStatus.Cancelled));
        }

        // Custom attributes filter
        if (filter.Attributes != null && filter.Attributes.Any())
        {
            var attrIndex = 0;
            foreach (var attr in filter.Attributes)
            {
                var attrJoinAlias = $"ta{attrIndex}";
                var attrDefJoinAlias = $"ad{attrIndex}";
                
                joins.Add($"LEFT JOIN task_attributes {attrJoinAlias} ON t.id = {attrJoinAlias}.task_id");
                joins.Add($"LEFT JOIN attribute_definitions {attrDefJoinAlias} ON {attrJoinAlias}.attribute_definition_id = {attrDefJoinAlias}.id");
                
                whereConditions.Add($"{attrDefJoinAlias}.name = @attrName{attrIndex} AND {attrJoinAlias}.value LIKE @attrValue{attrIndex}");
                parameters.Add(new SqliteParameter($"@attrName{attrIndex}", attr.Key));
                parameters.Add(new SqliteParameter($"@attrValue{attrIndex}", $"%{attr.Value}%"));
                
                attrIndex++;
            }
        }

        // Add joins
        foreach (var join in joins)
        {
            sql.AppendLine(join);
        }

        // Add WHERE clause
        if (whereConditions.Any())
        {
            sql.AppendLine("WHERE " + string.Join(" AND ", whereConditions));
        }

        // Add sorting
        sql.AppendLine(GetOrderByClause(filter.SortOrder, filter.SortDescending));

        // Add limit
        if (filter.Limit.HasValue)
        {
            sql.AppendLine($"LIMIT {filter.Limit.Value}");
        }

        return (sql.ToString(), parameters);
    }

    private (string sql, List<SqliteParameter> parameters) BuildListSearchQuery(SearchFilter filter)
    {
        var sql = new StringBuilder(@"
            SELECT DISTINCT tl.id, tl.name, tl.description, tl.parent_list_id, tl.created_at, tl.updated_at
            FROM task_lists tl");

        var parameters = new List<SqliteParameter>();
        var whereConditions = new List<string>();
        var joins = new List<string>();

        // Text search
        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            whereConditions.Add("(tl.name LIKE @query OR tl.description LIKE @query)");
            parameters.Add(new SqliteParameter("@query", $"%{filter.Query}%"));
        }

        // Tag filter for lists
        if (filter.Tags != null && filter.Tags.Any())
        {
            joins.Add("LEFT JOIN list_tags lt ON tl.id = lt.task_list_id");
            joins.Add("LEFT JOIN tags tag ON lt.tag_id = tag.id");
            
            var tagConditions = filter.Tags.Select((tag, index) =>
            {
                var paramName = $"@tag{index}";
                parameters.Add(new SqliteParameter(paramName, tag));
                return $"tag.name = {paramName}";
            });
            
            whereConditions.Add($"({string.Join(" OR ", tagConditions)})");
        }

        // Date filters
        if (filter.CreatedFrom.HasValue)
        {
            whereConditions.Add("tl.created_at >= @createdFrom");
            parameters.Add(new SqliteParameter("@createdFrom", filter.CreatedFrom.Value));
        }

        if (filter.CreatedTo.HasValue)
        {
            whereConditions.Add("tl.created_at <= @createdTo");
            parameters.Add(new SqliteParameter("@createdTo", filter.CreatedTo.Value));
        }

        // Add joins
        foreach (var join in joins)
        {
            sql.AppendLine(join);
        }

        // Add WHERE clause
        if (whereConditions.Any())
        {
            sql.AppendLine("WHERE " + string.Join(" AND ", whereConditions));
        }

        // Add sorting (simplified for lists)
        sql.AppendLine($"ORDER BY tl.name {(filter.SortDescending ? "DESC" : "ASC")}");

        // Add limit
        if (filter.Limit.HasValue)
        {
            sql.AppendLine($"LIMIT {filter.Limit.Value}");
        }

        return (sql.ToString(), parameters);
    }

    private string GetOrderByClause(SearchSortOrder sortOrder, bool descending)
    {
        var direction = descending ? "DESC" : "ASC";
        
        return sortOrder switch
        {
            SearchSortOrder.CreatedDate => $"ORDER BY t.created_at {direction}",
            SearchSortOrder.DueDate => $"ORDER BY t.due_date {direction} NULLS LAST",
            SearchSortOrder.Priority => $"ORDER BY t.priority {direction}, t.created_at {direction}",
            SearchSortOrder.Title => $"ORDER BY t.title {direction}",
            SearchSortOrder.UpdatedDate => $"ORDER BY t.updated_at {direction}",
            SearchSortOrder.Relevance => $"ORDER BY t.updated_at {direction}", // Default to updated date for relevance
            _ => $"ORDER BY t.created_at {direction}"
        };
    }

    #endregion
}
