using Microsoft.Extensions.Logging;
using TaskListMcp.Data;
using TaskListMcp.Models;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace TaskListMcp.Core.Services;

/// <summary>
/// Service for managing custom attributes
/// </summary>
public class AttributeService
{
    private readonly DatabaseManager _databaseManager;
    private readonly ILogger<AttributeService> _logger;

    public AttributeService(DatabaseManager databaseManager, ILogger<AttributeService> logger)
    {
        _databaseManager = databaseManager ?? throw new ArgumentNullException(nameof(databaseManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Attribute Definitions

    /// <summary>
    /// Create a new attribute definition
    /// </summary>
    public async Task<AttributeDefinition> CreateAttributeDefinitionAsync(
        string name, 
        AttributeType type, 
        bool isRequired = false, 
        string? defaultValue = null, 
        string? validationRules = null)
    {
        _logger.LogInformation("Creating attribute definition: {Name} (Type: {Type})", name, type);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Attribute name cannot be empty", nameof(name));

        // Validate JSON format of validation rules if provided
        if (!string.IsNullOrEmpty(validationRules))
        {
            try
            {
                JsonDocument.Parse(validationRules);
            }
            catch (JsonException)
            {
                throw new ArgumentException("Validation rules must be valid JSON", nameof(validationRules));
            }
        }

        var attributeDefinition = new AttributeDefinition
        {
            Name = name,
            Type = type,
            IsRequired = isRequired,
            DefaultValue = defaultValue,
            ValidationRules = validationRules,
            CreatedAt = DateTime.UtcNow
        };

        using var connection = await _databaseManager.GetConnectionAsync();
        connection.Open();

        var sql = @"
            INSERT INTO attribute_definitions (name, type, is_required, default_value, validation_rules, created_at)
            VALUES (@name, @type, @is_required, @default_value, @validation_rules, @created_at);
            SELECT last_insert_rowid();";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@type", (int)type);
        command.Parameters.AddWithValue("@is_required", isRequired);
        command.Parameters.AddWithValue("@default_value", (object?)defaultValue ?? DBNull.Value);
        command.Parameters.AddWithValue("@validation_rules", (object?)validationRules ?? DBNull.Value);
        command.Parameters.AddWithValue("@created_at", attributeDefinition.CreatedAt);

        var id = Convert.ToInt32(await command.ExecuteScalarAsync());
        attributeDefinition.Id = id;

        _logger.LogInformation("Created attribute definition with ID: {Id}", id);
        return attributeDefinition;
    }

    /// <summary>
    /// Get all attribute definitions
    /// </summary>
    public async Task<List<AttributeDefinition>> GetAllAttributeDefinitionsAsync()
    {
        _logger.LogDebug("Retrieving all attribute definitions");

        using var connection = await _databaseManager.GetConnectionAsync();
        connection.Open();

        var sql = @"
            SELECT id, name, type, is_required, default_value, validation_rules, created_at
            FROM attribute_definitions
            ORDER BY name";

        using var command = new SqliteCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        var definitions = new List<AttributeDefinition>();
        while (await reader.ReadAsync())
        {
            definitions.Add(new AttributeDefinition
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Type = (AttributeType)reader.GetInt32(reader.GetOrdinal("type")),
                IsRequired = reader.GetBoolean(reader.GetOrdinal("is_required")),
                DefaultValue = reader.IsDBNull(reader.GetOrdinal("default_value")) ? null : reader.GetString(reader.GetOrdinal("default_value")),
                ValidationRules = reader.IsDBNull(reader.GetOrdinal("validation_rules")) ? null : reader.GetString(reader.GetOrdinal("validation_rules")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            });
        }

        _logger.LogDebug("Retrieved {Count} attribute definitions", definitions.Count);
        return definitions;
    }

    /// <summary>
    /// Get attribute definition by ID
    /// </summary>
    public async Task<AttributeDefinition?> GetAttributeDefinitionByIdAsync(int id)
    {
        _logger.LogDebug("Retrieving attribute definition with ID: {Id}", id);

        using var connection = await _databaseManager.GetConnectionAsync();
        connection.Open();

        var sql = @"
            SELECT id, name, type, is_required, default_value, validation_rules, created_at
            FROM attribute_definitions
            WHERE id = @id";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new AttributeDefinition
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Type = (AttributeType)reader.GetInt32(reader.GetOrdinal("type")),
                IsRequired = reader.GetBoolean(reader.GetOrdinal("is_required")),
                DefaultValue = reader.IsDBNull(reader.GetOrdinal("default_value")) ? null : reader.GetString(reader.GetOrdinal("default_value")),
                ValidationRules = reader.IsDBNull(reader.GetOrdinal("validation_rules")) ? null : reader.GetString(reader.GetOrdinal("validation_rules")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            };
        }

        return null;
    }

    /// <summary>
    /// Get attribute definition by name
    /// </summary>
    public async Task<AttributeDefinition?> GetAttributeDefinitionByNameAsync(string name)
    {
        _logger.LogDebug("Retrieving attribute definition with name: {Name}", name);

        using var connection = await _databaseManager.GetConnectionAsync();
        connection.Open();

        var sql = @"
            SELECT id, name, type, is_required, default_value, validation_rules, created_at
            FROM attribute_definitions
            WHERE name = @name";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@name", name);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new AttributeDefinition
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Type = (AttributeType)reader.GetInt32(reader.GetOrdinal("type")),
                IsRequired = reader.GetBoolean(reader.GetOrdinal("is_required")),
                DefaultValue = reader.IsDBNull(reader.GetOrdinal("default_value")) ? null : reader.GetString(reader.GetOrdinal("default_value")),
                ValidationRules = reader.IsDBNull(reader.GetOrdinal("validation_rules")) ? null : reader.GetString(reader.GetOrdinal("validation_rules")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            };
        }

        return null;
    }

    /// <summary>
    /// Delete an attribute definition and all its values
    /// </summary>
    public async Task<bool> DeleteAttributeDefinitionAsync(int id)
    {
        _logger.LogInformation("Deleting attribute definition with ID: {Id}", id);

        using var connection = await _databaseManager.GetConnectionAsync();
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            // Delete all task attribute values
            var deleteTaskAttributesSql = "DELETE FROM task_attributes WHERE attribute_definition_id = @id";
            using var deleteTaskAttributesCommand = new SqliteCommand(deleteTaskAttributesSql, connection, transaction);
            deleteTaskAttributesCommand.Parameters.AddWithValue("@id", id);
            await deleteTaskAttributesCommand.ExecuteNonQueryAsync();

            // Delete all list attribute values
            var deleteListAttributesSql = "DELETE FROM list_attributes WHERE attribute_definition_id = @id";
            using var deleteListAttributesCommand = new SqliteCommand(deleteListAttributesSql, connection, transaction);
            deleteListAttributesCommand.Parameters.AddWithValue("@id", id);
            await deleteListAttributesCommand.ExecuteNonQueryAsync();

            // Delete the attribute definition
            var deleteDefinitionSql = "DELETE FROM attribute_definitions WHERE id = @id";
            using var deleteDefinitionCommand = new SqliteCommand(deleteDefinitionSql, connection, transaction);
            deleteDefinitionCommand.Parameters.AddWithValue("@id", id);
            var rowsAffected = await deleteDefinitionCommand.ExecuteNonQueryAsync();

            transaction.Commit();

            if (rowsAffected > 0)
            {
                _logger.LogInformation("Deleted attribute definition with ID: {Id}", id);
                return true;
            }

            _logger.LogWarning("Attribute definition with ID {Id} not found", id);
            return false;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "Error deleting attribute definition with ID: {Id}", id);
            throw;
        }
    }

    #endregion

    #region Task Attributes

    /// <summary>
    /// Set a task attribute value
    /// </summary>
    public async Task SetTaskAttributeAsync(int taskId, int attributeDefinitionId, string value)
    {
        _logger.LogInformation("Setting task attribute: TaskId={TaskId}, AttributeId={AttributeId}, Value={Value}", 
            taskId, attributeDefinitionId, value);

        // Validate the attribute definition exists
        var definition = await GetAttributeDefinitionByIdAsync(attributeDefinitionId);
        if (definition == null)
        {
            throw new ArgumentException($"Attribute definition with ID {attributeDefinitionId} not found");
        }

        // Validate the value according to the attribute type
        ValidateAttributeValue(definition, value);

        using var connection = await _databaseManager.GetConnectionAsync();
        connection.Open();

        // Check if attribute already exists for this task
        var checkSql = "SELECT COUNT(*) FROM task_attributes WHERE task_id = @task_id AND attribute_definition_id = @attr_id";
        using var checkCommand = new SqliteCommand(checkSql, connection);
        checkCommand.Parameters.AddWithValue("@task_id", taskId);
        checkCommand.Parameters.AddWithValue("@attr_id", attributeDefinitionId);
        var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

        if (exists)
        {
            // Update existing attribute
            var updateSql = @"
                UPDATE task_attributes 
                SET value = @value, updated_at = @updated_at
                WHERE task_id = @task_id AND attribute_definition_id = @attr_id";

            using var updateCommand = new SqliteCommand(updateSql, connection);
            updateCommand.Parameters.AddWithValue("@value", value);
            updateCommand.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
            updateCommand.Parameters.AddWithValue("@task_id", taskId);
            updateCommand.Parameters.AddWithValue("@attr_id", attributeDefinitionId);
            await updateCommand.ExecuteNonQueryAsync();
        }
        else
        {
            // Insert new attribute
            var insertSql = @"
                INSERT INTO task_attributes (task_id, attribute_definition_id, value, created_at, updated_at)
                VALUES (@task_id, @attr_id, @value, @created_at, @updated_at)";

            using var insertCommand = new SqliteCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("@task_id", taskId);
            insertCommand.Parameters.AddWithValue("@attr_id", attributeDefinitionId);
            insertCommand.Parameters.AddWithValue("@value", value);
            insertCommand.Parameters.AddWithValue("@created_at", DateTime.UtcNow);
            insertCommand.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
            await insertCommand.ExecuteNonQueryAsync();
        }

        _logger.LogInformation("Set task attribute successfully");
    }

    /// <summary>
    /// Get all attributes for a task
    /// </summary>
    public async Task<List<TaskAttribute>> GetTaskAttributesAsync(int taskId)
    {
        _logger.LogDebug("Getting attributes for task: {TaskId}", taskId);

        using var connection = await _databaseManager.GetConnectionAsync();
        connection.Open();

        var sql = @"
            SELECT ta.task_id, ta.attribute_definition_id, ta.value, ta.created_at, ta.updated_at,
                   ad.id as def_id, ad.name, ad.type, ad.is_required, ad.default_value, ad.validation_rules, ad.created_at as def_created_at
            FROM task_attributes ta
            JOIN attribute_definitions ad ON ta.attribute_definition_id = ad.id
            WHERE ta.task_id = @task_id
            ORDER BY ad.name";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@task_id", taskId);

        using var reader = await command.ExecuteReaderAsync();
        var attributes = new List<TaskAttribute>();

        while (await reader.ReadAsync())
        {
            attributes.Add(new TaskAttribute
            {
                TaskId = reader.GetInt32(reader.GetOrdinal("task_id")),
                AttributeDefinitionId = reader.GetInt32(reader.GetOrdinal("attribute_definition_id")),
                Value = reader.GetString(reader.GetOrdinal("value")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                AttributeDefinition = new AttributeDefinition
                {
                    Id = reader.GetInt32(reader.GetOrdinal("def_id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Type = (AttributeType)reader.GetInt32(reader.GetOrdinal("type")),
                    IsRequired = reader.GetBoolean(reader.GetOrdinal("is_required")),
                    DefaultValue = reader.IsDBNull(reader.GetOrdinal("default_value")) ? null : reader.GetString(reader.GetOrdinal("default_value")),
                    ValidationRules = reader.IsDBNull(reader.GetOrdinal("validation_rules")) ? null : reader.GetString(reader.GetOrdinal("validation_rules")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("def_created_at"))
                }
            });
        }

        _logger.LogDebug("Found {Count} attributes for task {TaskId}", attributes.Count, taskId);
        return attributes;
    }

    /// <summary>
    /// Remove a task attribute
    /// </summary>
    public async Task<bool> RemoveTaskAttributeAsync(int taskId, int attributeDefinitionId)
    {
        _logger.LogInformation("Removing task attribute: TaskId={TaskId}, AttributeId={AttributeId}", taskId, attributeDefinitionId);

        using var connection = await _databaseManager.GetConnectionAsync();
        connection.Open();

        var sql = "DELETE FROM task_attributes WHERE task_id = @task_id AND attribute_definition_id = @attr_id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@task_id", taskId);
        command.Parameters.AddWithValue("@attr_id", attributeDefinitionId);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        var success = rowsAffected > 0;

        if (success)
        {
            _logger.LogInformation("Removed task attribute successfully");
        }
        else
        {
            _logger.LogWarning("Task attribute not found for removal");
        }

        return success;
    }

    #endregion

    #region List Attributes

    /// <summary>
    /// Set a list attribute value
    /// </summary>
    public async Task SetListAttributeAsync(int listId, int attributeDefinitionId, string value)
    {
        _logger.LogInformation("Setting list attribute: ListId={ListId}, AttributeId={AttributeId}, Value={Value}", 
            listId, attributeDefinitionId, value);

        // Validate the attribute definition exists
        var definition = await GetAttributeDefinitionByIdAsync(attributeDefinitionId);
        if (definition == null)
        {
            throw new ArgumentException($"Attribute definition with ID {attributeDefinitionId} not found");
        }

        // Validate the value according to the attribute type
        ValidateAttributeValue(definition, value);

        using var connection = await _databaseManager.GetConnectionAsync();
        connection.Open();

        // Check if attribute already exists for this list
        var checkSql = "SELECT COUNT(*) FROM list_attributes WHERE task_list_id = @list_id AND attribute_definition_id = @attr_id";
        using var checkCommand = new SqliteCommand(checkSql, connection);
        checkCommand.Parameters.AddWithValue("@list_id", listId);
        checkCommand.Parameters.AddWithValue("@attr_id", attributeDefinitionId);
        var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

        if (exists)
        {
            // Update existing attribute
            var updateSql = @"
                UPDATE list_attributes 
                SET value = @value, updated_at = @updated_at
                WHERE task_list_id = @list_id AND attribute_definition_id = @attr_id";

            using var updateCommand = new SqliteCommand(updateSql, connection);
            updateCommand.Parameters.AddWithValue("@value", value);
            updateCommand.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
            updateCommand.Parameters.AddWithValue("@list_id", listId);
            updateCommand.Parameters.AddWithValue("@attr_id", attributeDefinitionId);
            await updateCommand.ExecuteNonQueryAsync();
        }
        else
        {
            // Insert new attribute
            var insertSql = @"
                INSERT INTO list_attributes (task_list_id, attribute_definition_id, value, created_at, updated_at)
                VALUES (@list_id, @attr_id, @value, @created_at, @updated_at)";

            using var insertCommand = new SqliteCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("@list_id", listId);
            insertCommand.Parameters.AddWithValue("@attr_id", attributeDefinitionId);
            insertCommand.Parameters.AddWithValue("@value", value);
            insertCommand.Parameters.AddWithValue("@created_at", DateTime.UtcNow);
            insertCommand.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
            await insertCommand.ExecuteNonQueryAsync();
        }

        _logger.LogInformation("Set list attribute successfully");
    }

    /// <summary>
    /// Get all attributes for a list
    /// </summary>
    public async Task<List<ListAttribute>> GetListAttributesAsync(int listId)
    {
        _logger.LogDebug("Getting attributes for list: {ListId}", listId);

        using var connection = await _databaseManager.GetConnectionAsync();
        connection.Open();

        var sql = @"
            SELECT la.task_list_id, la.attribute_definition_id, la.value, la.created_at, la.updated_at,
                   ad.id as def_id, ad.name, ad.type, ad.is_required, ad.default_value, ad.validation_rules, ad.created_at as def_created_at
            FROM list_attributes la
            JOIN attribute_definitions ad ON la.attribute_definition_id = ad.id
            WHERE la.task_list_id = @list_id
            ORDER BY ad.name";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@list_id", listId);

        using var reader = await command.ExecuteReaderAsync();
        var attributes = new List<ListAttribute>();

        while (await reader.ReadAsync())
        {
            attributes.Add(new ListAttribute
            {
                TaskListId = reader.GetInt32(reader.GetOrdinal("task_list_id")),
                AttributeDefinitionId = reader.GetInt32(reader.GetOrdinal("attribute_definition_id")),
                Value = reader.GetString(reader.GetOrdinal("value")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                AttributeDefinition = new AttributeDefinition
                {
                    Id = reader.GetInt32(reader.GetOrdinal("def_id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Type = (AttributeType)reader.GetInt32(reader.GetOrdinal("type")),
                    IsRequired = reader.GetBoolean(reader.GetOrdinal("is_required")),
                    DefaultValue = reader.IsDBNull(reader.GetOrdinal("default_value")) ? null : reader.GetString(reader.GetOrdinal("default_value")),
                    ValidationRules = reader.IsDBNull(reader.GetOrdinal("validation_rules")) ? null : reader.GetString(reader.GetOrdinal("validation_rules")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("def_created_at"))
                }
            });
        }

        _logger.LogDebug("Found {Count} attributes for list {ListId}", attributes.Count, listId);
        return attributes;
    }

    /// <summary>
    /// Remove a list attribute
    /// </summary>
    public async Task<bool> RemoveListAttributeAsync(int listId, int attributeDefinitionId)
    {
        _logger.LogInformation("Removing list attribute: ListId={ListId}, AttributeId={AttributeId}", listId, attributeDefinitionId);

        using var connection = await _databaseManager.GetConnectionAsync();
        connection.Open();

        var sql = "DELETE FROM list_attributes WHERE task_list_id = @list_id AND attribute_definition_id = @attr_id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@list_id", listId);
        command.Parameters.AddWithValue("@attr_id", attributeDefinitionId);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        var success = rowsAffected > 0;

        if (success)
        {
            _logger.LogInformation("Removed list attribute successfully");
        }
        else
        {
            _logger.LogWarning("List attribute not found for removal");
        }

        return success;
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validate an attribute value according to its definition
    /// </summary>
    private void ValidateAttributeValue(AttributeDefinition definition, string value)
    {
        if (definition.IsRequired && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Attribute '{definition.Name}' is required and cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return; // Empty values are valid for non-required attributes
        }

        switch (definition.Type)
        {
            case AttributeType.Integer:
                if (!int.TryParse(value, out _))
                {
                    throw new ArgumentException($"Attribute '{definition.Name}' requires an integer value");
                }
                break;

            case AttributeType.Decimal:
                if (!decimal.TryParse(value, out _))
                {
                    throw new ArgumentException($"Attribute '{definition.Name}' requires a decimal value");
                }
                break;

            case AttributeType.Date:
                if (!DateTime.TryParse(value, out _))
                {
                    throw new ArgumentException($"Attribute '{definition.Name}' requires a valid date");
                }
                break;

            case AttributeType.DateTime:
                if (!DateTime.TryParse(value, out _))
                {
                    throw new ArgumentException($"Attribute '{definition.Name}' requires a valid date and time");
                }
                break;

            case AttributeType.Boolean:
                if (!bool.TryParse(value, out _))
                {
                    throw new ArgumentException($"Attribute '{definition.Name}' requires a boolean value (true/false)");
                }
                break;

            case AttributeType.Url:
                if (!Uri.TryCreate(value, UriKind.Absolute, out _))
                {
                    throw new ArgumentException($"Attribute '{definition.Name}' requires a valid URL");
                }
                break;

            case AttributeType.SingleChoice:
            case AttributeType.MultipleChoice:
                // Validation would require checking against allowed choices defined in ValidationRules
                // For now, accept any string value
                break;

            case AttributeType.Text:
            case AttributeType.FileReference:
            default:
                // Accept any string value
                break;
        }

        // Additional validation based on ValidationRules could be implemented here
        // This would parse the JSON rules and apply specific constraints
    }

    #endregion
}
