using TaskListMcp.Core.Services;
using TaskListMcp.Models;

namespace TaskListMcp.Tests.Unit.Helpers;

/// <summary>
/// Helper class for building test data
/// </summary>
public class TestDataBuilder
{
    private readonly TaskService _taskService;

    public TestDataBuilder(TaskService taskService)
    {
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
    }

    /// <summary>
    /// Creates a test task with customizable parameters
    /// </summary>
    public async Task<TaskItem> CreateTestTaskAsync(
        string title = "Test Task",
        string? description = null,
        int listId = 1,
        Models.TaskStatus status = Models.TaskStatus.Pending,
        DateTime? dueDate = null,
        int? priority = null,
        decimal? estimatedHours = null)
    {
        return await _taskService.CreateTaskAsync(title, description, listId, status, dueDate, priority, estimatedHours);
    }

    /// <summary>
    /// Creates multiple test tasks
    /// </summary>
    public async Task<List<TaskItem>> CreateMultipleTestTasksAsync(
        int count = 5,
        int listId = 1,
        string titlePrefix = "Test Task")
    {
        var tasks = new List<TaskItem>();
        for (int i = 1; i <= count; i++)
        {
            var task = await CreateTestTaskAsync(
                title: $"{titlePrefix} {i}",
                description: $"Description for {titlePrefix} {i}",
                listId: listId);
            tasks.Add(task);
        }
        return tasks;
    }

    /// <summary>
    /// Creates a test task with in-progress status
    /// </summary>
    public async Task<TaskItem> CreateInProgressTaskAsync(
        string title = "In Progress Task",
        int listId = 1)
    {
        return await CreateTestTaskAsync(
            title: title,
            status: Models.TaskStatus.InProgress,
            listId: listId);
    }

    /// <summary>
    /// Creates a test task with completed status
    /// </summary>
    public async Task<TaskItem> CreateCompletedTaskAsync(
        string title = "Completed Task",
        int listId = 1)
    {
        return await CreateTestTaskAsync(
            title: title,
            status: Models.TaskStatus.Completed,
            listId: listId);
    }

    /// <summary>
    /// Creates a test task with due date
    /// </summary>
    public async Task<TaskItem> CreateTaskWithDueDateAsync(
        string title = "Task with Due Date",
        DateTime? dueDate = null,
        int listId = 1)
    {
        dueDate ??= DateTime.UtcNow.AddDays(7);
        return await CreateTestTaskAsync(
            title: title,
            dueDate: dueDate,
            listId: listId);
    }

    /// <summary>
    /// Creates a test task with priority
    /// </summary>
    public async Task<TaskItem> CreateTaskWithPriorityAsync(
        string title = "Priority Task",
        int priority = (int)Priority.High,
        int listId = 1)
    {
        return await CreateTestTaskAsync(
            title: title,
            priority: priority,
            listId: listId);
    }

    /// <summary>
    /// Creates a test task with estimated hours
    /// </summary>
    public async Task<TaskItem> CreateTaskWithEstimatedHoursAsync(
        string title = "Task with Estimated Hours",
        decimal estimatedHours = 4.5m,
        int listId = 1)
    {
        return await CreateTestTaskAsync(
            title: title,
            estimatedHours: estimatedHours,
            listId: listId);
    }

    /// <summary>
    /// Creates a large dataset for performance testing
    /// </summary>
    public async Task CreateLargeDatasetAsync(int taskCount = 1000, int listId = 1)
    {
        var tasks = new List<Task<TaskItem>>();
        
        for (int i = 0; i < taskCount; i++)
        {
            var task = CreateTestTaskAsync(
                title: $"Performance Test Task {i + 1}",
                description: $"This is a performance test task number {i + 1} with some description content",
                listId: listId,
                priority: i % 5, // Vary priority 0-4
                estimatedHours: (decimal)(Random.Shared.NextDouble() * 8 + 1) // 1-9 hours
            );
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Creates test data with various statuses
    /// </summary>
    public async Task<Dictionary<Models.TaskStatus, List<TaskItem>>> CreateTasksByStatusAsync(int perStatus = 3, int listId = 1)
    {
        var result = new Dictionary<Models.TaskStatus, List<TaskItem>>();
        var statuses = Enum.GetValues<Models.TaskStatus>();

        foreach (var status in statuses)
        {
            var tasks = new List<TaskItem>();
            for (int i = 0; i < perStatus; i++)
            {
                var task = await CreateTestTaskAsync(
                    title: $"{status} Task {i + 1}",
                    status: status,
                    listId: listId);
                tasks.Add(task);
            }
            result[status] = tasks;
        }

        return result;
    }
}
