using Microsoft.Data.Sqlite;

namespace TaskListMcp.Data;

/// <summary>
/// Factory for creating database connections with proper lifecycle management
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates and opens a new database connection
    /// </summary>
    Task<SqliteConnection> CreateConnectionAsync();
    
    /// <summary>
    /// Gets the connection string
    /// </summary>
    string ConnectionString { get; }
}

/// <summary>
/// SQLite implementation of database connection factory
/// </summary>
public class SqliteConnectionFactory : IDbConnectionFactory
{
    public string ConnectionString { get; }

    public SqliteConnectionFactory(string connectionString)
    {
        ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Creates and opens a new SQLite connection
    /// </summary>
    public async Task<SqliteConnection> CreateConnectionAsync()
    {
        var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        
        // Enable foreign keys and optimize settings
        using var command = connection.CreateCommand();
        command.CommandText = @"
            PRAGMA foreign_keys = ON;
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;
            PRAGMA temp_store = MEMORY;
            PRAGMA mmap_size = 268435456;";
        await command.ExecuteNonQueryAsync();
        
        return connection;
    }
}
