using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using TaskListMcp.Data;
using TaskListMcp.Models;

namespace TaskListMcp.Core.Services;

/// <summary>
/// Service for managing task lists operations
/// </summary>
public class ListService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<ListService> _logger;

    public ListService(IDbConnectionFactory connectionFactory, ILogger<ListService> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger;
    }

    /// <summary>
    /// Creates a new task list
    /// </summary>
    public async Task<TaskList> CreateListAsync(string name, string? description = null, int? parentListId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("List name cannot be empty", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("List name cannot exceed 200 characters", nameof(name));

        // Validate parent list exists if specified
        if (parentListId.HasValue)
        {
            var parentExists = await ListExistsAsync(parentListId.Value);
            if (!parentExists)
                throw new ArgumentException($"Parent list with ID {parentListId} does not exist", nameof(parentListId));

            // Check for circular reference by verifying the parent isn't a child of any future children
            await ValidateNoCircularReferenceAsync(parentListId.Value, parentListId.Value);
        }

        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            INSERT INTO task_lists (name, description, parent_list_id, created_at, updated_at)
            VALUES (@name, @description, @parentListId, @createdAt, @updatedAt);
            SELECT last_insert_rowid();";

        var now = DateTime.UtcNow;
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@description", description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@parentListId", parentListId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@createdAt", now);
        command.Parameters.AddWithValue("@updatedAt", now);

        var listId = Convert.ToInt32(await command.ExecuteScalarAsync());
        
        _logger.LogInformation("Created list '{Name}' with ID {ListId}", name, listId);

        return new TaskList
        {
            Id = listId,
            Name = name,
            Description = description,
            ParentListId = parentListId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Updates an existing task list
    /// </summary>
    public async Task<TaskList> UpdateListAsync(int listId, string? name = null, string? description = null, int? parentListId = null)
    {
        var existingList = await GetListByIdAsync(listId);
        if (existingList == null)
            throw new ArgumentException($"List with ID {listId} does not exist", nameof(listId));

        // Validate name if provided
        if (name != null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("List name cannot be empty", nameof(name));
            if (name.Length > 200)
                throw new ArgumentException("List name cannot exceed 200 characters", nameof(name));
        }

        // Validate parent list if changing
        if (parentListId.HasValue && parentListId != existingList.ParentListId)
        {
            if (parentListId == listId)
                throw new ArgumentException("List cannot be its own parent", nameof(parentListId));

            var parentExists = await ListExistsAsync(parentListId.Value);
            if (!parentExists)
                throw new ArgumentException($"Parent list with ID {parentListId} does not exist", nameof(parentListId));

            // Check for circular reference
            await ValidateNoCircularReferenceAsync(listId, parentListId.Value);
        }

        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            UPDATE task_lists 
            SET name = COALESCE(@name, name),
                description = CASE WHEN @description IS NULL AND @hasDescription = 0 THEN description ELSE @description END,
                parent_list_id = CASE WHEN @parentListId IS NULL AND @hasParentListId = 0 THEN parent_list_id ELSE @parentListId END,
                updated_at = @updatedAt
            WHERE id = @listId";

        var now = DateTime.UtcNow;
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@listId", listId);
        command.Parameters.AddWithValue("@name", name ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@description", description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@hasDescription", description != null ? 1 : 0);
        command.Parameters.AddWithValue("@parentListId", parentListId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@hasParentListId", parentListId.HasValue ? 1 : 0);
        command.Parameters.AddWithValue("@updatedAt", now);

        await command.ExecuteNonQueryAsync();
        
        _logger.LogInformation("Updated list with ID {ListId}", listId);

        // Return updated list
        return await GetListByIdAsync(listId) ?? throw new InvalidOperationException("Failed to retrieve updated list");
    }

    /// <summary>
    /// Deletes a task list and handles cleanup
    /// </summary>
    public async Task<bool> DeleteListAsync(int listId, bool cascadeDelete = false)
    {
        var existingList = await GetListByIdAsync(listId);
        if (existingList == null)
            return false;

        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Check for child lists
            var hasChildren = await HasChildListsAsync(listId, connection);
            if (hasChildren && !cascadeDelete)
            {
                throw new InvalidOperationException($"List {listId} has child lists. Use cascade delete to remove them or move them first.");
            }

            // Check for tasks
            var taskCount = await GetTaskCountInListAsync(listId, connection);
            if (taskCount > 0 && !cascadeDelete)
            {
                throw new InvalidOperationException($"List {listId} contains {taskCount} tasks. Use cascade delete to remove them or move them first.");
            }

            if (cascadeDelete)
            {
                // Delete child lists recursively
                if (hasChildren)
                {
                    await DeleteChildListsRecursivelyAsync(listId, connection);
                }

                // Delete or orphan tasks (set list_id to NULL)
                const string orphanTasksSql = "UPDATE tasks SET list_id = NULL WHERE list_id = @listId";
                using var orphanCommand = new SqliteCommand(orphanTasksSql, connection, transaction);
                orphanCommand.Parameters.AddWithValue("@listId", listId);
                await orphanCommand.ExecuteNonQueryAsync();
            }

            // Delete the list itself
            const string deleteSql = "DELETE FROM task_lists WHERE id = @listId";
            using var deleteCommand = new SqliteCommand(deleteSql, connection, transaction);
            deleteCommand.Parameters.AddWithValue("@listId", listId);
            await deleteCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            
            _logger.LogInformation("Deleted list with ID {ListId} (cascade: {Cascade})", listId, cascadeDelete);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Gets a task list by ID
    /// </summary>
    public async Task<TaskList?> GetListByIdAsync(int listId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT l.id, l.name, l.description, l.parent_list_id, l.created_at, l.updated_at,
                   p.name as parent_name,
                   (SELECT COUNT(*) FROM tasks WHERE list_id = l.id) as task_count,
                   (SELECT COUNT(*) FROM task_lists WHERE parent_list_id = l.id) as child_list_count
            FROM task_lists l
            LEFT JOIN task_lists p ON l.parent_list_id = p.id
            WHERE l.id = @listId";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@listId", listId);

        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new TaskList
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
            ParentListId = reader.IsDBNull(reader.GetOrdinal("parent_list_id")) ? null : reader.GetInt32(reader.GetOrdinal("parent_list_id")),
            ParentListName = reader.IsDBNull(reader.GetOrdinal("parent_name")) ? null : reader.GetString(reader.GetOrdinal("parent_name")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
            TaskCount = reader.GetInt32(reader.GetOrdinal("task_count")),
            ChildListCount = reader.GetInt32(reader.GetOrdinal("child_list_count"))
        };
    }

    /// <summary>
    /// Lists all task lists with optional hierarchy
    /// </summary>
    public async Task<List<TaskList>> GetAllListsAsync(bool hierarchical = false)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT l.id, l.name, l.description, l.parent_list_id, l.created_at, l.updated_at,
                   p.name as parent_name,
                   (SELECT COUNT(*) FROM tasks WHERE list_id = l.id) as task_count,
                   (SELECT COUNT(*) FROM task_lists WHERE parent_list_id = l.id) as child_list_count
            FROM task_lists l
            LEFT JOIN task_lists p ON l.parent_list_id = p.id
            ORDER BY COALESCE(l.parent_list_id, l.id), l.name";

        using var command = new SqliteCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        var lists = new List<TaskList>();
        while (await reader.ReadAsync())
        {
            var list = new TaskList
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                ParentListId = reader.IsDBNull(reader.GetOrdinal("parent_list_id")) ? null : reader.GetInt32(reader.GetOrdinal("parent_list_id")),
                ParentListName = reader.IsDBNull(reader.GetOrdinal("parent_name")) ? null : reader.GetString(reader.GetOrdinal("parent_name")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                TaskCount = reader.GetInt32(reader.GetOrdinal("task_count")),
                ChildListCount = reader.GetInt32(reader.GetOrdinal("child_list_count"))
            };

            lists.Add(list);
        }

        if (hierarchical)
        {
            return BuildHierarchy(lists);
        }

        return lists;
    }

    /// <summary>
    /// Moves a task to a different list
    /// </summary>
    public async Task<bool> MoveTaskToListAsync(int taskId, int? targetListId)
    {
        // Validate target list exists if specified
        if (targetListId.HasValue)
        {
            var targetExists = await ListExistsAsync(targetListId.Value);
            if (!targetExists)
                throw new ArgumentException($"Target list with ID {targetListId} does not exist", nameof(targetListId));
        }

        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            UPDATE tasks 
            SET list_id = @targetListId, updated_at = @updatedAt
            WHERE id = @taskId";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@taskId", taskId);
        command.Parameters.AddWithValue("@targetListId", targetListId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        
        if (rowsAffected > 0)
        {
            _logger.LogInformation("Moved task {TaskId} to list {TargetListId}", taskId, targetListId);
        }

        return rowsAffected > 0;
    }

    #region Private Helper Methods

    private async Task<bool> ListExistsAsync(int listId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = "SELECT COUNT(*) FROM task_lists WHERE id = @listId";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@listId", listId);
        
        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        return count > 0;
    }

    private async Task ValidateNoCircularReferenceAsync(int listId, int proposedParentId)
    {
        var ancestors = new HashSet<int>();
        int? currentParentId = proposedParentId;

        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        while (currentParentId.HasValue)
        {
            if (currentParentId.Value == listId)
                throw new InvalidOperationException("Circular reference detected: list cannot be an ancestor of itself");

            if (ancestors.Contains(currentParentId.Value))
                throw new InvalidOperationException("Circular reference detected in parent hierarchy");

            ancestors.Add(currentParentId.Value);

            const string sql = "SELECT parent_list_id FROM task_lists WHERE id = @listId";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@listId", currentParentId.Value);
            
            var result = await command.ExecuteScalarAsync();
            currentParentId = result == DBNull.Value ? null : Convert.ToInt32(result);
        }
    }

    private async Task<bool> HasChildListsAsync(int listId, SqliteConnection connection)
    {
        const string sql = "SELECT COUNT(*) FROM task_lists WHERE parent_list_id = @listId";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@listId", listId);
        
        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        return count > 0;
    }

    private async Task<int> GetTaskCountInListAsync(int listId, SqliteConnection connection)
    {
        const string sql = "SELECT COUNT(*) FROM tasks WHERE list_id = @listId";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@listId", listId);
        
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private async Task DeleteChildListsRecursivelyAsync(int parentListId, SqliteConnection connection)
    {
        const string getChildrenSql = "SELECT id FROM task_lists WHERE parent_list_id = @parentListId";
        using var command = new SqliteCommand(getChildrenSql, connection);
        command.Parameters.AddWithValue("@parentListId", parentListId);
        
        using var reader = await command.ExecuteReaderAsync();
        var childIds = new List<int>();
        while (await reader.ReadAsync())
        {
            childIds.Add(reader.GetInt32(0));
        }
        reader.Close();

        // Recursively delete children
        foreach (var childId in childIds)
        {
            await DeleteChildListsRecursivelyAsync(childId, connection);
            
            const string deleteSql = "DELETE FROM task_lists WHERE id = @childId";
            using var deleteCommand = new SqliteCommand(deleteSql, connection);
            deleteCommand.Parameters.AddWithValue("@childId", childId);
            await deleteCommand.ExecuteNonQueryAsync();
        }
    }

    private static List<TaskList> BuildHierarchy(List<TaskList> flatLists)
    {
        var rootLists = flatLists.Where(l => l.ParentListId == null).ToList();
        var childLookup = flatLists.Where(l => l.ParentListId != null)
                                  .GroupBy(l => l.ParentListId!.Value)
                                  .ToDictionary(g => g.Key, g => g.ToList());

        void PopulateChildren(TaskList parent)
        {
            if (childLookup.TryGetValue(parent.Id, out var children))
            {
                parent.ChildLists = children;
                foreach (var child in children)
                {
                    PopulateChildren(child);
                }
            }
        }

        foreach (var root in rootLists)
        {
            PopulateChildren(root);
        }

        return rootLists;
    }

    #endregion
}
