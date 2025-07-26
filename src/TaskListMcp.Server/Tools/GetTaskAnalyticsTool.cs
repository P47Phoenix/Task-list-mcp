using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TaskListMcp.Core.Services;
using System.ComponentModel;

namespace TaskListMcp.Server.Tools;

/// <summary>
/// MCP tool for getting task analytics and insights
/// </summary>
[McpServerToolType]
public class GetTaskAnalyticsTool
{
    /// <summary>
    /// Get analytics and insights about tasks and productivity
    /// </summary>
    [McpServerTool(Name = "get_task_analytics")]
    [Description("Get analytics and insights about tasks and productivity")]
    public static async Task<object> GetTaskAnalyticsAsync(
        [Description("Optional list ID to get analytics for specific list (null for all tasks)")] int? listId = null,
        [Description("Maximum number of top tags to show (default: 10)")] int maxTags = 10,
        SearchService searchService = null!,
        ILogger<GetTaskAnalyticsTool> logger = null!)
    {
        try
        {
            logger.LogInformation("Getting task analytics for list: {ListId}", listId);

            // Get task counts by status
            var statusCounts = await searchService.GetTaskCountByStatusAsync(listId);

            // Get most used tags
            var topTags = await searchService.GetMostUsedTagsAsync(maxTags);

            // Calculate totals and percentages
            var totalTasks = statusCounts.Values.Sum();
            var completedTasks = statusCounts.GetValueOrDefault(Models.TaskStatus.Completed, 0);
            var inProgressTasks = statusCounts.GetValueOrDefault(Models.TaskStatus.InProgress, 0);
            var pendingTasks = statusCounts.GetValueOrDefault(Models.TaskStatus.Pending, 0);
            var blockedTasks = statusCounts.GetValueOrDefault(Models.TaskStatus.Blocked, 0);
            var cancelledTasks = statusCounts.GetValueOrDefault(Models.TaskStatus.Cancelled, 0);

            var completionRate = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0;
            var activeTasksRate = totalTasks > 0 ? (double)(inProgressTasks + pendingTasks) / totalTasks * 100 : 0;

            return new
            {
                success = true,
                message = $"Analytics for {(listId.HasValue ? $"list {listId}" : "all tasks")}",
                listId = listId,
                overview = new
                {
                    totalTasks = totalTasks,
                    completionRate = Math.Round(completionRate, 1),
                    activeTasksRate = Math.Round(activeTasksRate, 1)
                },
                tasksByStatus = new
                {
                    pending = pendingTasks,
                    inProgress = inProgressTasks,
                    completed = completedTasks,
                    blocked = blockedTasks,
                    cancelled = cancelledTasks
                },
                statusPercentages = new
                {
                    pending = totalTasks > 0 ? Math.Round((double)pendingTasks / totalTasks * 100, 1) : 0,
                    inProgress = totalTasks > 0 ? Math.Round((double)inProgressTasks / totalTasks * 100, 1) : 0,
                    completed = totalTasks > 0 ? Math.Round((double)completedTasks / totalTasks * 100, 1) : 0,
                    blocked = totalTasks > 0 ? Math.Round((double)blockedTasks / totalTasks * 100, 1) : 0,
                    cancelled = totalTasks > 0 ? Math.Round((double)cancelledTasks / totalTasks * 100, 1) : 0
                },
                topTags = topTags.Select(tag => new
                {
                    name = tag.TagName,
                    usageCount = tag.UsageCount
                }).ToList(),
                insights = GenerateInsights(totalTasks, completionRate, activeTasksRate, inProgressTasks, blockedTasks)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting task analytics");
            return new
            {
                success = false,
                error = ex.Message
            };
        }
    }

    private static List<string> GenerateInsights(int totalTasks, double completionRate, double activeTasksRate, int inProgressTasks, int blockedTasks)
    {
        var insights = new List<string>();

        if (totalTasks == 0)
        {
            insights.Add("No tasks found. Consider creating some tasks to get started!");
            return insights;
        }

        if (completionRate > 80)
        {
            insights.Add($"Excellent completion rate of {completionRate:F1}%! You're very productive.");
        }
        else if (completionRate > 60)
        {
            insights.Add($"Good completion rate of {completionRate:F1}%. Keep up the good work!");
        }
        else if (completionRate > 40)
        {
            insights.Add($"Completion rate is {completionRate:F1}%. Consider focusing on completing existing tasks.");
        }
        else
        {
            insights.Add($"Low completion rate of {completionRate:F1}%. You might have too many tasks in progress.");
        }

        if (inProgressTasks > 5)
        {
            insights.Add($"You have {inProgressTasks} tasks in progress. Consider focusing on fewer tasks at once for better productivity.");
        }
        else if (inProgressTasks == 1)
        {
            insights.Add("Great focus! You have exactly one task in progress.");
        }

        if (blockedTasks > 0)
        {
            insights.Add($"You have {blockedTasks} blocked tasks. Review these to see if any can be unblocked.");
        }

        if (activeTasksRate > 70)
        {
            insights.Add($"{activeTasksRate:F1}% of your tasks are active (pending or in progress). This shows good engagement with your task list.");
        }

        return insights;
    }
}
