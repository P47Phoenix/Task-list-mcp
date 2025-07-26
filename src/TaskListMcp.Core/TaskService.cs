using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using TaskListMcp.Data;
using TaskListMcp.Models;

namespace TaskListMcp.Core.Services;

/// <summary>
/// Service for managing tasks in the task management system
/// </summary>
public class TaskService
{
    private readonly DatabaseManager _databaseManager;
    private readonly ILogger<TaskService> _logger;

    public TaskService(DatabaseManager databaseManager, ILogger<TaskService> logger)
    {
        _databaseManager = databaseManager ?? throw new ArgumentNullException(nameof(databaseManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new task
    /// </summary>
    public async Task<TaskItem> CreateTaskAsync(string title, string? description = null, int listId = 1, Models.TaskStatus status = Models.TaskStatus.Pending, DateTime? dueDate = null, int? priority = null, decimal? estimatedHours = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Task title cannot be empty", nameof(title));
        }

        _logger.LogInformation("Creating new task: {Title}", title);

        var connection = await _databaseManager.GetConnectionAsync();
        
        // Verify the list exists
        await ValidateListExistsAsync(connection, listId);
        
        // If setting status to InProgress, check for existing active tasks in the list
        if (status == Models.TaskStatus.InProgress)
        {
            await EnsureOnlyOneActiveTaskPerListAsync(connection, listId);
        }

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO tasks (title, description, status, list_id, created_at, updated_at, due_date, priority, estimated_hours)
            VALUES (@title, @description, @status, @listId, @createdAt, @updatedAt, @dueDate, @priority, @estimatedHours);
            SELECT last_insert_rowid();";
        
        command.Parameters.AddWithValue("@title", title);
        command.Parameters.AddWithValue("@description", description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@status", (int)status);
        command.Parameters.AddWithValue("@listId", listId);
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        command.Parameters.AddWithValue("@createdAt", now);
        command.Parameters.AddWithValue("@updatedAt", now);
        command.Parameters.AddWithValue("@dueDate", dueDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@priority", priority ?? 0);
        command.Parameters.AddWithValue("@estimatedHours", estimatedHours ?? (object)DBNull.Value);

        var taskId = Convert.ToInt32(await command.ExecuteScalarAsync());
        
        _logger.LogInformation("Created task with ID: {TaskId}", taskId);
        
        return new TaskItem
        {
            Id = taskId,
            Title = title,
            Description = description,
            Status = status,
            ListId = listId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates an existing task
    /// </summary>
    public async Task<TaskItem> UpdateTaskAsync(int taskId, string? title = null, string? description = null, Models.TaskStatus? status = null, int? listId = null, DateTime? dueDate = null, Priority? priority = null, decimal? estimatedHours = null)
    {
        _logger.LogInformation("Updating task: {TaskId}", taskId);

        var connection = await _databaseManager.GetConnectionAsync();
        
        // First, get the current task to verify it exists
        var currentTask = await GetTaskByIdAsync(taskId);
        if (currentTask == null)
        {
            throw new ArgumentException($"Task with ID {taskId} not found", nameof(taskId));
        }

        // If changing status to InProgress, check for existing active tasks in the target list
        var targetListId = listId ?? currentTask.ListId;
        if (status == Models.TaskStatus.InProgress && status != currentTask.Status)
        {
            await EnsureOnlyOneActiveTaskPerListAsync(connection, targetListId, taskId);
        }

        // If moving to a different list, validate it exists
        if (listId.HasValue && listId != currentTask.ListId)
        {
            await ValidateListExistsAsync(connection, listId.Value);
        }

        var updates = new List<string>();
        var command = connection.CreateCommand();
        
        if (!string.IsNullOrEmpty(title))
        {
            updates.Add("title = @title");
            command.Parameters.AddWithValue("@title", title);
        }
        
        if (description != null)
        {
            updates.Add("description = @description");
            command.Parameters.AddWithValue("@description", string.IsNullOrEmpty(description) ? DBNull.Value : description);
        }
        
        if (status.HasValue)
        {
            updates.Add("status = @status");
            command.Parameters.AddWithValue("@status", (int)status.Value);
        }
        
        if (listId.HasValue)
        {
            updates.Add("list_id = @listId");
            command.Parameters.AddWithValue("@listId", listId.Value);
        }

        if (dueDate.HasValue)
        {
            updates.Add("due_date = @dueDate");
            command.Parameters.AddWithValue("@dueDate", dueDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        
        if (priority.HasValue)
        {
            updates.Add("priority = @priority");
            command.Parameters.AddWithValue("@priority", (int)priority.Value);
        }
        
        if (estimatedHours.HasValue)
        {
            updates.Add("estimated_hours = @estimatedHours");
            command.Parameters.AddWithValue("@estimatedHours", estimatedHours.Value);
        }

        if (updates.Count == 0)
        {
            return currentTask; // Nothing to update
        }

        updates.Add("updated_at = @updatedAt");
        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@taskId", taskId);

        command.CommandText = $@"
            UPDATE tasks 
            SET {string.Join(", ", updates)}
            WHERE id = @taskId AND deleted_at IS NULL";

        var rowsAffected = await command.ExecuteNonQueryAsync();
        
        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"Failed to update task {taskId}");
        }

        _logger.LogInformation("Updated task: {TaskId}", taskId);
        
        // Return updated task
        return await GetTaskByIdAsync(taskId) ?? currentTask;
    }

    /// <summary>
    /// Gets a task by ID
    /// </summary>
    public async Task<TaskItem?> GetTaskByIdAsync(int taskId)
    {
        var connection = await _databaseManager.GetConnectionAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT t.id, t.title, t.description, t.status, t.list_id, t.created_at, t.updated_at, 
                   t.due_date, t.priority, t.estimated_hours, l.name as list_name
            FROM tasks t
            LEFT JOIN task_lists l ON t.list_id = l.id
            WHERE t.id = @taskId AND t.deleted_at IS NULL";
        command.Parameters.AddWithValue("@taskId", taskId);

        using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            return new TaskItem
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                Status = (Models.TaskStatus)reader.GetInt32(reader.GetOrdinal("status")),
                ListId = reader.GetInt32(reader.GetOrdinal("list_id")),
                ListName = reader.IsDBNull(reader.GetOrdinal("list_name")) ? null : reader.GetString(reader.GetOrdinal("list_name")),
                CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at"))),
                UpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("updated_at"))),
                DueDate = reader.IsDBNull(reader.GetOrdinal("due_date")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("due_date"))),
                Priority = (Priority)reader.GetInt32(reader.GetOrdinal("priority")),
                EstimatedHours = reader.IsDBNull(reader.GetOrdinal("estimated_hours")) ? null : reader.GetDecimal(reader.GetOrdinal("estimated_hours"))
            };
        }

        return null;
    }

    /// <summary>
    /// Starts a task (sets status to InProgress)
    /// </summary>
    public async Task<TaskItem> StartTaskAsync(int taskId)
    {
        _logger.LogInformation("Starting task: {TaskId}", taskId);
        return await UpdateTaskAsync(taskId, status: Models.TaskStatus.InProgress);
    }

    /// <summary>
    /// Completes a task (sets status to Completed)
    /// </summary>
    public async Task<TaskItem> CompleteTaskAsync(int taskId)
    {
        _logger.LogInformation("Completing task: {TaskId}", taskId);
        return await UpdateTaskAsync(taskId, status: Models.TaskStatus.Completed);
    }

    /// <summary>
    /// Soft deletes a task
    /// </summary>
    public async Task<bool> DeleteTaskAsync(int taskId)
    {
        _logger.LogInformation("Deleting task: {TaskId}", taskId);

        var connection = await _databaseManager.GetConnectionAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE tasks 
            SET deleted_at = @deletedAt, updated_at = @updatedAt
            WHERE id = @taskId AND deleted_at IS NULL";
        
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        command.Parameters.AddWithValue("@deletedAt", now);
        command.Parameters.AddWithValue("@updatedAt", now);
        command.Parameters.AddWithValue("@taskId", taskId);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        
        _logger.LogInformation("Deleted task: {TaskId}, Success: {Success}", taskId, rowsAffected > 0);
        
        return rowsAffected > 0;
    }

    /// <summary>
    /// Lists tasks with optional filtering
    /// </summary>
    public async Task<List<TaskItem>> ListTasksAsync(int? listId = null, Models.TaskStatus? status = null, int? limit = null, int? offset = null)
    {
        var connection = await _databaseManager.GetConnectionAsync();
        
        var whereClauses = new List<string> { "deleted_at IS NULL" };
        var command = connection.CreateCommand();
        
        if (listId.HasValue)
        {
            whereClauses.Add("list_id = @listId");
            command.Parameters.AddWithValue("@listId", listId.Value);
        }
        
        if (status.HasValue)
        {
            whereClauses.Add("status = @status");
            command.Parameters.AddWithValue("@status", (int)status.Value);
        }

        var sql = $@"
            SELECT id, title, description, status, list_id, created_at, updated_at, due_date, priority, estimated_hours
            FROM tasks 
            WHERE {string.Join(" AND ", whereClauses)}
            ORDER BY created_at DESC";

        if (limit.HasValue)
        {
            sql += " LIMIT @limit";
            command.Parameters.AddWithValue("@limit", limit.Value);
            
            if (offset.HasValue)
            {
                sql += " OFFSET @offset";
                command.Parameters.AddWithValue("@offset", offset.Value);
            }
        }

        command.CommandText = sql;

        var tasks = new List<TaskItem>();
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            tasks.Add(new TaskItem
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                Status = (Models.TaskStatus)reader.GetInt32(reader.GetOrdinal("status")),
                ListId = reader.GetInt32(reader.GetOrdinal("list_id")),
                CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at"))),
                UpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("updated_at"))),
                DueDate = reader.IsDBNull(reader.GetOrdinal("due_date")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("due_date"))),
                Priority = (Priority)reader.GetInt32(reader.GetOrdinal("priority")),
                EstimatedHours = reader.IsDBNull(reader.GetOrdinal("estimated_hours")) ? null : reader.GetDecimal(reader.GetOrdinal("estimated_hours"))
            });
        }

        return tasks;
    }

    private async Task ValidateListExistsAsync(SqliteConnection connection, int listId)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM task_lists WHERE id = @listId AND deleted_at IS NULL";
        command.Parameters.AddWithValue("@listId", listId);
        
        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        
        if (count == 0)
        {
            throw new ArgumentException($"Task list with ID {listId} not found", nameof(listId));
        }
    }

    private async Task EnsureOnlyOneActiveTaskPerListAsync(SqliteConnection connection, int listId, int? excludeTaskId = null)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE tasks 
            SET status = @pendingStatus, updated_at = @updatedAt
            WHERE list_id = @listId AND status = @inProgressStatus AND deleted_at IS NULL";
        
        if (excludeTaskId.HasValue)
        {
            command.CommandText += " AND id != @excludeTaskId";
            command.Parameters.AddWithValue("@excludeTaskId", excludeTaskId.Value);
        }
        
        command.Parameters.AddWithValue("@listId", listId);
        command.Parameters.AddWithValue("@inProgressStatus", (int)Models.TaskStatus.InProgress);
        command.Parameters.AddWithValue("@pendingStatus", (int)Models.TaskStatus.Pending);
        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

        var pausedTasks = await command.ExecuteNonQueryAsync();
        
        if (pausedTasks > 0)
        {
            _logger.LogInformation("Paused {Count} tasks in list {ListId} to maintain single active task constraint", pausedTasks, listId);
        }
    }
}
