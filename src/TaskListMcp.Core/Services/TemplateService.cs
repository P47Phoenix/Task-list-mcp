using TaskListMcp.Data;
using TaskListMcp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using TaskStatus = TaskListMcp.Models.TaskStatus;

namespace TaskListMcp.Core.Services;

/// <summary>
/// Service for managing templates and template operations
/// </summary>
public class TemplateService
{
    private readonly DatabaseManager _databaseManager;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(DatabaseManager databaseManager, ILogger<TemplateService> logger)
    {
        _databaseManager = databaseManager;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new template from an existing list
    /// </summary>
    public async Task<Template?> CreateTemplateFromListAsync(int listId, string templateName, string? templateDescription = null, string? category = null)
    {
        try
        {
            var connection = await _databaseManager.GetConnectionAsync();

            // Check if the source list exists
            var listCommand = connection.CreateCommand();
            listCommand.CommandText = "SELECT COUNT(*) FROM task_lists WHERE id = @listId AND deleted_at IS NULL";
            listCommand.Parameters.AddWithValue("@listId", listId);
            
            var listExists = (long)(await listCommand.ExecuteScalarAsync() ?? 0) > 0;
            if (!listExists)
            {
                _logger.LogWarning("Source list {ListId} not found or deleted", listId);
                return null;
            }

            // Create the template
            var templateCommand = connection.CreateCommand();
            templateCommand.CommandText = @"
                INSERT INTO templates (name, description, category, created_at, updated_at)
                VALUES (@name, @description, @category, @createdAt, @updatedAt);
                SELECT last_insert_rowid();";
            
            templateCommand.Parameters.AddWithValue("@name", templateName);
            templateCommand.Parameters.AddWithValue("@description", templateDescription ?? (object)DBNull.Value);
            templateCommand.Parameters.AddWithValue("@category", category ?? (object)DBNull.Value);
            templateCommand.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            templateCommand.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            var templateId = Convert.ToInt32(await templateCommand.ExecuteScalarAsync());

            // Copy tasks from the list to template tasks
            var copyTasksCommand = connection.CreateCommand();
            copyTasksCommand.CommandText = @"
                INSERT INTO template_tasks (template_id, title, description, order_index, estimated_hours, priority, created_at)
                SELECT @templateId, title, description, 
                       ROW_NUMBER() OVER (ORDER BY created_at) - 1 as order_index,
                       estimated_hours, priority, @createdAt
                FROM tasks 
                WHERE list_id = @listId AND deleted_at IS NULL
                ORDER BY created_at";

            copyTasksCommand.Parameters.AddWithValue("@templateId", templateId);
            copyTasksCommand.Parameters.AddWithValue("@listId", listId);
            copyTasksCommand.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            await copyTasksCommand.ExecuteNonQueryAsync();

            _logger.LogInformation("Created template {TemplateName} from list {ListId}", templateName, listId);

            // Retrieve and return the created template
            return await GetTemplateByIdAsync(templateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template from list {ListId}", listId);
            return null;
        }
    }

    /// <summary>
    /// Creates a new template manually
    /// </summary>
    public async Task<Template?> CreateTemplateAsync(string name, string? description = null, string? category = null)
    {
        try
        {
            var connection = await _databaseManager.GetConnectionAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO templates (name, description, category, created_at, updated_at)
                VALUES (@name, @description, @category, @createdAt, @updatedAt);
                SELECT last_insert_rowid();";
            
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@description", description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@category", category ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            var templateId = Convert.ToInt32(await command.ExecuteScalarAsync());

            _logger.LogInformation("Created template {TemplateName} with ID {TemplateId}", name, templateId);

            return await GetTemplateByIdAsync(templateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template {TemplateName}", name);
            return null;
        }
    }

    /// <summary>
    /// Gets a template by ID with all its tasks
    /// </summary>
    public async Task<Template?> GetTemplateByIdAsync(int templateId)
    {
        try
        {
            var connection = await _databaseManager.GetConnectionAsync();

            // Get template details
            var templateCommand = connection.CreateCommand();
            templateCommand.CommandText = @"
                SELECT id, name, description, category, version, created_at, updated_at, deleted_at
                FROM templates 
                WHERE id = @templateId AND deleted_at IS NULL";
            templateCommand.Parameters.AddWithValue("@templateId", templateId);

            Template? template = null;
            using (var reader = await templateCommand.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    template = new Template
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                        Category = reader.IsDBNull(reader.GetOrdinal("category")) ? null : reader.GetString(reader.GetOrdinal("category")),
                        Version = reader.GetString(reader.GetOrdinal("version")),
                        CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at"))),
                        UpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("updated_at"))),
                        DeletedAt = reader.IsDBNull(reader.GetOrdinal("deleted_at")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("deleted_at")))
                    };
                }
            }

            if (template == null)
            {
                return null;
            }

            // Get template tasks
            var tasksCommand = connection.CreateCommand();
            tasksCommand.CommandText = @"
                SELECT id, template_id, title, description, order_index, estimated_hours, priority, created_at
                FROM template_tasks 
                WHERE template_id = @templateId
                ORDER BY order_index";
            tasksCommand.Parameters.AddWithValue("@templateId", templateId);

            using (var reader = await tasksCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var templateTask = new TemplateTask
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        TemplateId = reader.GetInt32(reader.GetOrdinal("template_id")),
                        Title = reader.GetString(reader.GetOrdinal("title")),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                        OrderIndex = reader.GetInt32(reader.GetOrdinal("order_index")),
                        EstimatedHours = reader.IsDBNull(reader.GetOrdinal("estimated_hours")) ? null : (decimal)reader.GetDouble(reader.GetOrdinal("estimated_hours")),
                        Priority = (Priority)reader.GetInt32(reader.GetOrdinal("priority")),
                        CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at"))),
                        Template = template
                    };
                    template.Tasks.Add(templateTask);
                }
            }

            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template {TemplateId}", templateId);
            return null;
        }
    }

    /// <summary>
    /// Gets all templates with basic information (without tasks)
    /// </summary>
    public async Task<List<Template>> GetAllTemplatesAsync(string? category = null)
    {
        try
        {
            var templates = new List<Template>();
            var connection = await _databaseManager.GetConnectionAsync();

            var command = connection.CreateCommand();
            var whereClause = "WHERE deleted_at IS NULL";
            
            if (!string.IsNullOrEmpty(category))
            {
                whereClause += " AND category = @category";
            }

            command.CommandText = $@"
                SELECT t.id, t.name, t.description, t.category, t.version, t.created_at, t.updated_at,
                       COUNT(tt.id) as task_count
                FROM templates t
                LEFT JOIN template_tasks tt ON t.id = tt.template_id
                {whereClause}
                GROUP BY t.id, t.name, t.description, t.category, t.version, t.created_at, t.updated_at
                ORDER BY t.created_at DESC";

            if (!string.IsNullOrEmpty(category))
            {
                command.Parameters.AddWithValue("@category", category);
            }

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var template = new Template
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                        Category = reader.IsDBNull(reader.GetOrdinal("category")) ? null : reader.GetString(reader.GetOrdinal("category")),
                        Version = reader.GetString(reader.GetOrdinal("version")),
                        CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at"))),
                        UpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("updated_at")))
                    };
                    templates.Add(template);
                }
            }

            return templates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving templates");
            return new List<Template>();
        }
    }

    /// <summary>
    /// Applies a template to create a new list with tasks
    /// </summary>
    public async Task<TaskList?> ApplyTemplateAsync(int templateId, string listName, string? listDescription = null, int? parentListId = null)
    {
        try
        {
            var connection = await _databaseManager.GetConnectionAsync();

            // Get the template
            var template = await GetTemplateByIdAsync(templateId);
            if (template == null)
            {
                _logger.LogWarning("Template {TemplateId} not found", templateId);
                return null;
            }

            // Create the new list
            var listCommand = connection.CreateCommand();
            listCommand.CommandText = @"
                INSERT INTO task_lists (name, description, parent_id, created_at, updated_at)
                VALUES (@name, @description, @parentId, @createdAt, @updatedAt);
                SELECT last_insert_rowid();";
            
            listCommand.Parameters.AddWithValue("@name", listName);
            listCommand.Parameters.AddWithValue("@description", listDescription ?? template.Description ?? (object)DBNull.Value);
            listCommand.Parameters.AddWithValue("@parentId", parentListId ?? (object)DBNull.Value);
            listCommand.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            listCommand.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            var listId = Convert.ToInt32(await listCommand.ExecuteScalarAsync());

            // Create tasks from template
            foreach (var templateTask in template.Tasks.OrderBy(t => t.OrderIndex))
            {
                var taskCommand = connection.CreateCommand();
                taskCommand.CommandText = @"
                    INSERT INTO tasks (title, description, status, list_id, priority, estimated_hours, created_at, updated_at)
                    VALUES (@title, @description, @status, @listId, @priority, @estimatedHours, @createdAt, @updatedAt)";
                
                taskCommand.Parameters.AddWithValue("@title", templateTask.Title);
                taskCommand.Parameters.AddWithValue("@description", templateTask.Description ?? (object)DBNull.Value);
                taskCommand.Parameters.AddWithValue("@status", (int)TaskStatus.Pending);
                taskCommand.Parameters.AddWithValue("@listId", listId);
                taskCommand.Parameters.AddWithValue("@priority", templateTask.Priority);
                taskCommand.Parameters.AddWithValue("@estimatedHours", templateTask.EstimatedHours ?? (object)DBNull.Value);
                taskCommand.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                taskCommand.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

                await taskCommand.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("Applied template {TemplateName} to create list {ListName} with {TaskCount} tasks", 
                template.Name, listName, template.Tasks.Count);

            // Return the created list (you'll need to implement GetListByIdAsync in ListService)
            // For now, return a basic TaskList object
            return new TaskList
            {
                Id = listId,
                Name = listName,
                Description = listDescription ?? template.Description,
                ParentListId = parentListId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying template {TemplateId}", templateId);
            return null;
        }
    }

    /// <summary>
    /// Deletes a template (soft delete)
    /// </summary>
    public async Task<bool> DeleteTemplateAsync(int templateId)
    {
        try
        {
            var connection = await _databaseManager.GetConnectionAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE templates 
                SET deleted_at = @deletedAt, updated_at = @updatedAt
                WHERE id = @templateId AND deleted_at IS NULL";
            
            command.Parameters.AddWithValue("@templateId", templateId);
            command.Parameters.AddWithValue("@deletedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            var rowsAffected = await command.ExecuteNonQueryAsync();
            var success = rowsAffected > 0;

            if (success)
            {
                _logger.LogInformation("Deleted template {TemplateId}", templateId);
            }
            else
            {
                _logger.LogWarning("Template {TemplateId} not found or already deleted", templateId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId}", templateId);
            return false;
        }
    }
}
