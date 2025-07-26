using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for getting search suggestions
/// </summary>
[McpServerToolType]
public class GetSearchSuggestionsTool
{
    /// <summary>
    /// Get search suggestions for auto-complete functionality
    /// </summary>
    [McpServerTool(Name = "get_search_suggestions")]
    [Description("Get search suggestions for auto-complete functionality")]
    public static async Task<object> GetSearchSuggestionsAsync(
        [Description("Partial query to get suggestions for (minimum 2 characters)")] string partialQuery,
        [Description("Maximum number of suggestions to return (default: 10)")] int maxSuggestions = 10,
        SearchService searchService = null!,
        ILogger<GetSearchSuggestionsTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Getting search suggestions for: {Query}", partialQuery);

            if (string.IsNullOrWhiteSpace(partialQuery) || partialQuery.Length < 2)
            {
                return new
                {
                    success = false,
                    error = "Partial query must be at least 2 characters long"
                };
            }

            var suggestions = await searchService.GetSearchSuggestionsAsync(partialQuery, maxSuggestions);

            return new
            {
                success = true,
                message = $"Found {suggestions.Count} search suggestions",
                partialQuery = partialQuery,
                suggestions = suggestions
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting search suggestions");
            return new
            {
                success = false,
                error = ex.Message
            };
        }
    }
}
