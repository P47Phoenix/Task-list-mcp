using Microsoft.Data.Sqlite;
using TaskListMcp.Models;

namespace TaskListMcp.Data;

/// <summary>
/// Manages database connections and initialization for the Task List MCP Server
/// </summary>
public class DatabaseManager : IDisposable
{
    private readonly DatabaseConfig _config;
    private SqliteConnection? _connection;
    private bool _disposed = false;

    public DatabaseManager(DatabaseConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Gets or creates a database connection
    /// </summary>
    public async Task<SqliteConnection> GetConnectionAsync()
    {
        if (_connection == null)
        {
            _connection = new SqliteConnection(_config.ConnectionString);
            await _connection.OpenAsync();
            
            if (_config.EnableForeignKeys)
            {
                var command = _connection.CreateCommand();
                command.CommandText = "PRAGMA foreign_keys = ON";
                await command.ExecuteNonQueryAsync();
            }
        }
        
        return _connection;
    }

    /// <summary>
    /// Initializes the database with required tables
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        var connection = await GetConnectionAsync();
        
        // Create tables
        await CreateTablesAsync(connection);
        
        // Create indexes for performance
        await CreateIndexesAsync(connection);
        
        // Insert default data if needed
        await InsertDefaultDataAsync(connection);
    }

    private async Task CreateTablesAsync(SqliteConnection connection)
    {
        var commands = new[]
        {
            // Task Lists table
            @"CREATE TABLE IF NOT EXISTS task_lists (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                description TEXT,
                parent_id INTEGER,
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                updated_at TEXT NOT NULL DEFAULT (datetime('now')),
                deleted_at TEXT,
                FOREIGN KEY (parent_id) REFERENCES task_lists (id)
            )",
            
            // Tasks table
            @"CREATE TABLE IF NOT EXISTS tasks (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL,
                description TEXT,
                status INTEGER NOT NULL DEFAULT 0,
                list_id INTEGER NOT NULL,
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                updated_at TEXT NOT NULL DEFAULT (datetime('now')),
                deleted_at TEXT,
                due_date TEXT,
                priority INTEGER NOT NULL DEFAULT 0,
                estimated_hours REAL,
                FOREIGN KEY (list_id) REFERENCES task_lists (id)
            )",
            
            // Tags table
            @"CREATE TABLE IF NOT EXISTS tags (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                color TEXT,
                parent_id INTEGER,
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                FOREIGN KEY (parent_id) REFERENCES tags (id)
            )",
            
            // Task-Tag junction table
            @"CREATE TABLE IF NOT EXISTS task_tags (
                task_id INTEGER NOT NULL,
                tag_id INTEGER NOT NULL,
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                PRIMARY KEY (task_id, tag_id),
                FOREIGN KEY (task_id) REFERENCES tasks (id) ON DELETE CASCADE,
                FOREIGN KEY (tag_id) REFERENCES tags (id) ON DELETE CASCADE
            )",
            
            // List-Tag junction table
            @"CREATE TABLE IF NOT EXISTS list_tags (
                list_id INTEGER NOT NULL,
                tag_id INTEGER NOT NULL,
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                PRIMARY KEY (list_id, tag_id),
                FOREIGN KEY (list_id) REFERENCES task_lists (id) ON DELETE CASCADE,
                FOREIGN KEY (tag_id) REFERENCES tags (id) ON DELETE CASCADE
            )",
            
            // Attribute Definitions table
            @"CREATE TABLE IF NOT EXISTS attribute_definitions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                type INTEGER NOT NULL,
                validation_rules TEXT,
                default_value TEXT,
                is_required INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL DEFAULT (datetime('now'))
            )",
            
            // Task Attributes table
            @"CREATE TABLE IF NOT EXISTS task_attributes (
                task_id INTEGER NOT NULL,
                attribute_definition_id INTEGER NOT NULL,
                value TEXT NOT NULL,
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                updated_at TEXT NOT NULL DEFAULT (datetime('now')),
                PRIMARY KEY (task_id, attribute_definition_id),
                FOREIGN KEY (task_id) REFERENCES tasks (id) ON DELETE CASCADE,
                FOREIGN KEY (attribute_definition_id) REFERENCES attribute_definitions (id)
            )",
            
            // List Attributes table
            @"CREATE TABLE IF NOT EXISTS list_attributes (
                list_id INTEGER NOT NULL,
                attribute_definition_id INTEGER NOT NULL,
                value TEXT NOT NULL,
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                updated_at TEXT NOT NULL DEFAULT (datetime('now')),
                PRIMARY KEY (list_id, attribute_definition_id),
                FOREIGN KEY (list_id) REFERENCES task_lists (id) ON DELETE CASCADE,
                FOREIGN KEY (attribute_definition_id) REFERENCES attribute_definitions (id)
            )",
            
            // Templates table
            @"CREATE TABLE IF NOT EXISTS templates (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                description TEXT,
                category TEXT,
                version TEXT NOT NULL DEFAULT '1.0',
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                updated_at TEXT NOT NULL DEFAULT (datetime('now')),
                deleted_at TEXT
            )",
            
            // Template Tasks table
            @"CREATE TABLE IF NOT EXISTS template_tasks (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                template_id INTEGER NOT NULL,
                title TEXT NOT NULL,
                description TEXT,
                order_index INTEGER NOT NULL DEFAULT 0,
                estimated_hours REAL,
                priority INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                FOREIGN KEY (template_id) REFERENCES templates (id) ON DELETE CASCADE
            )"
        };

        foreach (var commandText in commands)
        {
            var command = connection.CreateCommand();
            command.CommandText = commandText;
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task CreateIndexesAsync(SqliteConnection connection)
    {
        var indexes = new[]
        {
            "CREATE INDEX IF NOT EXISTS idx_tasks_list_id ON tasks (list_id)",
            "CREATE INDEX IF NOT EXISTS idx_tasks_status ON tasks (status)",
            "CREATE INDEX IF NOT EXISTS idx_tasks_due_date ON tasks (due_date)",
            "CREATE INDEX IF NOT EXISTS idx_task_lists_parent_id ON task_lists (parent_id)",
            "CREATE INDEX IF NOT EXISTS idx_tags_parent_id ON tags (parent_id)",
            "CREATE INDEX IF NOT EXISTS idx_tasks_deleted_at ON tasks (deleted_at)",
            "CREATE INDEX IF NOT EXISTS idx_task_lists_deleted_at ON task_lists (deleted_at)"
        };

        foreach (var indexText in indexes)
        {
            var command = connection.CreateCommand();
            command.CommandText = indexText;
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task InsertDefaultDataAsync(SqliteConnection connection)
    {
        // Check if we already have a default list
        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT COUNT(*) FROM task_lists WHERE name = 'Default'";
        var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
        
        if (count == 0)
        {
            // Insert default task list
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO task_lists (name, description) 
                VALUES ('Default', 'Default task list for new tasks')";
            await insertCommand.ExecuteNonQueryAsync();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
