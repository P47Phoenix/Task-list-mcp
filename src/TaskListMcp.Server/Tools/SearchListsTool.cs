using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for searching task lists
/// </summary>
[McpServerToolType]
public class SearchListsTool
{
    /// <summary>
    /// Search task lists with full-text search and filtering
    /// </summary>
    [McpServerTool(Name = "search_lists")]
    [Description("Search task lists with full-text search and filtering")]
    public static async Task<object> SearchListsAsync(
        [Description("Search query for list name and description")] string? query = null,
        [Description("Comma-separated list of tag names to filter by")] string? tags = null,
        [Description("Filter by creation date from (YYYY-MM-DD format)")] string? createdFrom = null,
        [Description("Filter by creation date to (YYYY-MM-DD format)")] string? createdTo = null,
        [Description("Maximum number of results (default: 50)")] int limit = 50,
        [Description("Sort in descending order (default: true)")] bool sortDescending = true,
        SearchService searchService = null!,
        ILogger<SearchListsTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Searching lists: Query={Query}, Tags={Tags}", query, tags);

            var filter = new SearchFilter
            {
                Query = query,
                Limit = limit,
                SortDescending = sortDescending
            };

            // Parse tags
            if (!string.IsNullOrEmpty(tags))
            {
                filter.Tags = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(t => t.Trim())
                                 .ToList();
            }

            // Parse dates
            if (!string.IsNullOrEmpty(createdFrom) && DateTime.TryParse(createdFrom, out var fromDate))
            {
                filter.CreatedFrom = fromDate;
            }

            if (!string.IsNullOrEmpty(createdTo) && DateTime.TryParse(createdTo, out var toDate))
            {
                filter.CreatedTo = toDate;
            }

            var lists = await searchService.SearchListsAsync(filter);

            return new
            {
                success = true,
                message = $"Found {lists.Count} lists matching search criteria",
                totalResults = lists.Count,
                searchCriteria = new
                {
                    query = query,
                    tags = tags,
                    createdFrom = createdFrom,
                    createdTo = createdTo,
                    limit = limit,
                    sortDescending = sortDescending
                },
                lists = lists.Select(l => new
                {
                    id = l.Id,
                    name = l.Name,
                    description = l.Description,
                    parentListId = l.ParentListId,
                    createdAt = l.CreatedAt,
                    updatedAt = l.UpdatedAt
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching lists");
            return new
            {
                success = false,
                error = ex.Message
            };
        }
    }
}
