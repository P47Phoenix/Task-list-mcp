using TaskListMcp.Data;
using TaskListMcp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;

namespace TaskListMcp.Core.Services;

/// <summary>
/// Service for managing tags and tag operations
/// </summary>
public class TagService
{
    private readonly DatabaseManager _databaseManager;
    private readonly ILogger<TagService> _logger;

    public TagService(DatabaseManager databaseManager, ILogger<TagService> logger)
    {
        _databaseManager = databaseManager;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new tag
    /// </summary>
    public async Task<Tag?> CreateTagAsync(string name, string? color = null, int? parentId = null)
    {
        try
        {
            var connection = await _databaseManager.GetConnectionAsync();

            // Check if tag name already exists
            var existsCommand = connection.CreateCommand();
            existsCommand.CommandText = "SELECT COUNT(*) FROM tags WHERE name = @name";
            existsCommand.Parameters.AddWithValue("@name", name);
            
            var exists = (long)(await existsCommand.ExecuteScalarAsync() ?? 0) > 0;
            if (exists)
            {
                _logger.LogWarning("Tag with name '{TagName}' already exists", name);
                return null;
            }

            // Validate parent exists if specified
            if (parentId.HasValue)
            {
                var parentCommand = connection.CreateCommand();
                parentCommand.CommandText = "SELECT COUNT(*) FROM tags WHERE id = @parentId";
                parentCommand.Parameters.AddWithValue("@parentId", parentId.Value);
                
                var parentExists = (long)(await parentCommand.ExecuteScalarAsync() ?? 0) > 0;
                if (!parentExists)
                {
                    _logger.LogWarning("Parent tag with ID {ParentId} not found", parentId.Value);
                    return null;
                }
            }

            // Create the tag
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO tags (name, color, parent_id, created_at)
                VALUES (@name, @color, @parentId, @createdAt);
                SELECT last_insert_rowid();";
            
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@color", color ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@parentId", parentId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            var tagId = Convert.ToInt32(await command.ExecuteScalarAsync());

            _logger.LogInformation("Created tag '{TagName}' with ID {TagId}", name, tagId);

            return await GetTagByIdAsync(tagId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tag '{TagName}'", name);
            return null;
        }
    }

    /// <summary>
    /// Gets a tag by ID
    /// </summary>
    public async Task<Tag?> GetTagByIdAsync(int tagId)
    {
        try
        {
            var connection = await _databaseManager.GetConnectionAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, name, color, parent_id, created_at
                FROM tags 
                WHERE id = @tagId";
            command.Parameters.AddWithValue("@tagId", tagId);

            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    return new Tag
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        Color = reader.IsDBNull(reader.GetOrdinal("color")) ? null : reader.GetString(reader.GetOrdinal("color")),
                        ParentId = reader.IsDBNull(reader.GetOrdinal("parent_id")) ? null : reader.GetInt32(reader.GetOrdinal("parent_id")),
                        CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at")))
                    };
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tag {TagId}", tagId);
            return null;
        }
    }

    /// <summary>
    /// Gets all tags with usage statistics
    /// </summary>
    public async Task<List<Tag>> GetAllTagsAsync(bool includeHierarchy = false)
    {
        try
        {
            var tags = new List<Tag>();
            var connection = await _databaseManager.GetConnectionAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT t.id, t.name, t.color, t.parent_id, t.created_at,
                       COUNT(DISTINCT tt.task_id) as task_count,
                       COUNT(DISTINCT lt.list_id) as list_count
                FROM tags t
                LEFT JOIN task_tags tt ON t.id = tt.tag_id
                LEFT JOIN list_tags lt ON t.id = lt.tag_id
                GROUP BY t.id, t.name, t.color, t.parent_id, t.created_at
                ORDER BY t.name";

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var tag = new Tag
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        Color = reader.IsDBNull(reader.GetOrdinal("color")) ? null : reader.GetString(reader.GetOrdinal("color")),
                        ParentId = reader.IsDBNull(reader.GetOrdinal("parent_id")) ? null : reader.GetInt32(reader.GetOrdinal("parent_id")),
                        CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at")))
                    };
                    tags.Add(tag);
                }
            }

            // Build hierarchy if requested
            if (includeHierarchy)
            {
                var tagDict = tags.ToDictionary(t => t.Id);
                foreach (var tag in tags)
                {
                    if (tag.ParentId.HasValue && tagDict.ContainsKey(tag.ParentId.Value))
                    {
                        tag.Parent = tagDict[tag.ParentId.Value];
                        tag.Parent.Children.Add(tag);
                    }
                }
            }

            return tags;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tags");
            return new List<Tag>();
        }
    }

    /// <summary>
    /// Adds a tag to a task
    /// </summary>
    public async Task<bool> AddTagToTaskAsync(int taskId, int tagId)
    {
        try
        {
            var connection = await _databaseManager.GetConnectionAsync();

            // Check if task exists
            var taskCommand = connection.CreateCommand();
            taskCommand.CommandText = "SELECT COUNT(*) FROM tasks WHERE id = @taskId AND deleted_at IS NULL";
            taskCommand.Parameters.AddWithValue("@taskId", taskId);
            
            var taskExists = (long)(await taskCommand.ExecuteScalarAsync() ?? 0) > 0;
            if (!taskExists)
            {
                _logger.LogWarning("Task {TaskId} not found", taskId);
                return false;
            }

            // Check if tag exists
            var tagCommand = connection.CreateCommand();
            tagCommand.CommandText = "SELECT COUNT(*) FROM tags WHERE id = @tagId";
            tagCommand.Parameters.AddWithValue("@tagId", tagId);
            
            var tagExists = (long)(await tagCommand.ExecuteScalarAsync() ?? 0) > 0;
            if (!tagExists)
            {
                _logger.LogWarning("Tag {TagId} not found", tagId);
                return false;
            }

            // Add tag to task (ignore if already exists)
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR IGNORE INTO task_tags (task_id, tag_id, created_at)
                VALUES (@taskId, @tagId, @createdAt)";
            
            command.Parameters.AddWithValue("@taskId", taskId);
            command.Parameters.AddWithValue("@tagId", tagId);
            command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("Added tag {TagId} to task {TaskId}", tagId, taskId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tag {TagId} to task {TaskId}", tagId, taskId);
            return false;
        }
    }

    /// <summary>
    /// Removes a tag from a task
    /// </summary>
    public async Task<bool> RemoveTagFromTaskAsync(int taskId, int tagId)
    {
        try
        {
            var connection = await _databaseManager.GetConnectionAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM task_tags 
                WHERE task_id = @taskId AND tag_id = @tagId";
            
            command.Parameters.AddWithValue("@taskId", taskId);
            command.Parameters.AddWithValue("@tagId", tagId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            var success = rowsAffected > 0;

            if (success)
            {
                _logger.LogInformation("Removed tag {TagId} from task {TaskId}", tagId, taskId);
            }
            else
            {
                _logger.LogWarning("Tag {TagId} was not associated with task {TaskId}", tagId, taskId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tag {TagId} from task {TaskId}", tagId, taskId);
            return false;
        }
    }

    /// <summary>
    /// Adds a tag to a list
    /// </summary>
    public async Task<bool> AddTagToListAsync(int listId, int tagId)
    {
        try
        {
            var connection = await _databaseManager.GetConnectionAsync();

            // Check if list exists
            var listCommand = connection.CreateCommand();
            listCommand.CommandText = "SELECT COUNT(*) FROM task_lists WHERE id = @listId AND deleted_at IS NULL";
            listCommand.Parameters.AddWithValue("@listId", listId);
            
            var listExists = (long)(await listCommand.ExecuteScalarAsync() ?? 0) > 0;
            if (!listExists)
            {
                _logger.LogWarning("List {ListId} not found", listId);
                return false;
            }

            // Check if tag exists
            var tagCommand = connection.CreateCommand();
            tagCommand.CommandText = "SELECT COUNT(*) FROM tags WHERE id = @tagId";
            tagCommand.Parameters.AddWithValue("@tagId", tagId);
            
            var tagExists = (long)(await tagCommand.ExecuteScalarAsync() ?? 0) > 0;
            if (!tagExists)
            {
                _logger.LogWarning("Tag {TagId} not found", tagId);
                return false;
            }

            // Add tag to list (ignore if already exists)
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR IGNORE INTO list_tags (list_id, tag_id, created_at)
                VALUES (@listId, @tagId, @createdAt)";
            
            command.Parameters.AddWithValue("@listId", listId);
            command.Parameters.AddWithValue("@tagId", tagId);
            command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("Added tag {TagId} to list {ListId}", tagId, listId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tag {TagId} to list {ListId}", tagId, listId);
            return false;
        }
    }

    /// <summary>
    /// Removes a tag from a list
    /// </summary>
    public async Task<bool> RemoveTagFromListAsync(int listId, int tagId)
    {
        try
        {
            var connection = await _databaseManager.GetConnectionAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM list_tags 
                WHERE list_id = @listId AND tag_id = @tagId";
            
            command.Parameters.AddWithValue("@listId", listId);
            command.Parameters.AddWithValue("@tagId", tagId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            var success = rowsAffected > 0;

            if (success)
            {
                _logger.LogInformation("Removed tag {TagId} from list {ListId}", tagId, listId);
            }
            else
            {
                _logger.LogWarning("Tag {TagId} was not associated with list {ListId}", tagId, listId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tag {TagId} from list {ListId}", tagId, listId);
            return false;
        }
    }

    /// <summary>
    /// Gets tags for a specific task
    /// </summary>
    public async Task<List<Tag>> GetTaskTagsAsync(int taskId)
    {
        try
        {
            var tags = new List<Tag>();
            var connection = await _databaseManager.GetConnectionAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT t.id, t.name, t.color, t.parent_id, t.created_at
                FROM tags t
                INNER JOIN task_tags tt ON t.id = tt.tag_id
                WHERE tt.task_id = @taskId
                ORDER BY t.name";
            command.Parameters.AddWithValue("@taskId", taskId);

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var tag = new Tag
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        Color = reader.IsDBNull(reader.GetOrdinal("color")) ? null : reader.GetString(reader.GetOrdinal("color")),
                        ParentId = reader.IsDBNull(reader.GetOrdinal("parent_id")) ? null : reader.GetInt32(reader.GetOrdinal("parent_id")),
                        CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at")))
                    };
                    tags.Add(tag);
                }
            }

            return tags;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tags for task {TaskId}", taskId);
            return new List<Tag>();
        }
    }

    /// <summary>
    /// Gets tags for a specific list
    /// </summary>
    public async Task<List<Tag>> GetListTagsAsync(int listId)
    {
        try
        {
            var tags = new List<Tag>();
            var connection = await _databaseManager.GetConnectionAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT t.id, t.name, t.color, t.parent_id, t.created_at
                FROM tags t
                INNER JOIN list_tags lt ON t.id = lt.tag_id
                WHERE lt.list_id = @listId
                ORDER BY t.name";
            command.Parameters.AddWithValue("@listId", listId);

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var tag = new Tag
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        Color = reader.IsDBNull(reader.GetOrdinal("color")) ? null : reader.GetString(reader.GetOrdinal("color")),
                        ParentId = reader.IsDBNull(reader.GetOrdinal("parent_id")) ? null : reader.GetInt32(reader.GetOrdinal("parent_id")),
                        CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at")))
                    };
                    tags.Add(tag);
                }
            }

            return tags;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tags for list {ListId}", listId);
            return new List<Tag>();
        }
    }

    /// <summary>
    /// Deletes a tag and removes all associations
    /// </summary>
    public async Task<bool> DeleteTagAsync(int tagId)
    {
        try
        {
            var connection = await _databaseManager.GetConnectionAsync();

            // Check if tag exists
            var existsCommand = connection.CreateCommand();
            existsCommand.CommandText = "SELECT COUNT(*) FROM tags WHERE id = @tagId";
            existsCommand.Parameters.AddWithValue("@tagId", tagId);
            
            var exists = (long)(await existsCommand.ExecuteScalarAsync() ?? 0) > 0;
            if (!exists)
            {
                _logger.LogWarning("Tag {TagId} not found", tagId);
                return false;
            }

            // Delete the tag (cascade will handle associations)
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM tags WHERE id = @tagId";
            command.Parameters.AddWithValue("@tagId", tagId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            var success = rowsAffected > 0;

            if (success)
            {
                _logger.LogInformation("Deleted tag {TagId}", tagId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tag {TagId}", tagId);
            return false;
        }
    }
}
