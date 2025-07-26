namespace TaskListMcp.Models;

/// <summary>
/// Configuration settings for the Task List MCP Server
/// </summary>
public class TaskListMcpConfig
{
    /// <summary>
    /// Database configuration
    /// </summary>
    public DatabaseConfig Database { get; set; } = new();
    
    /// <summary>
    /// Server configuration
    /// </summary>
    public ServerConfig Server { get; set; } = new();
    
    /// <summary>
    /// Feature flags
    /// </summary>
    public FeatureFlags Features { get; set; } = new();
}

/// <summary>
/// Database configuration settings
/// </summary>
public class DatabaseConfig
{
    /// <summary>
    /// Path to the SQLite database file
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=tasklist.db";
    
    /// <summary>
    /// Whether to create the database if it doesn't exist
    /// </summary>
    public bool CreateIfNotExists { get; set; } = true;
    
    /// <summary>
    /// Whether to enable foreign key constraints
    /// </summary>
    public bool EnableForeignKeys { get; set; } = true;
}

/// <summary>
/// Server configuration settings
/// </summary>
public class ServerConfig
{
    /// <summary>
    /// Server name for MCP
    /// </summary>
    public string Name { get; set; } = "TaskList MCP Server";
    
    /// <summary>
    /// Server version
    /// </summary>
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>
    /// Maximum number of tasks that can be returned in a single query
    /// </summary>
    public int MaxTasksPerQuery { get; set; } = 100;
    
    /// <summary>
    /// Maximum depth for nested task lists
    /// </summary>
    public int MaxListDepth { get; set; } = 5;
}

/// <summary>
/// Feature flags to enable/disable specific features
/// </summary>
public class FeatureFlags
{
    /// <summary>
    /// Whether to enable the template system
    /// </summary>
    public bool EnableTemplates { get; set; } = true;
    
    /// <summary>
    /// Whether to enable custom attributes
    /// </summary>
    public bool EnableCustomAttributes { get; set; } = true;
    
    /// <summary>
    /// Whether to enable hierarchical tags
    /// </summary>
    public bool EnableHierarchicalTags { get; set; } = true;
    
    /// <summary>
    /// Whether to enable task dependencies
    /// </summary>
    public bool EnableTaskDependencies { get; set; } = true;
    
    /// <summary>
    /// Whether to enable analytics
    /// </summary>
    public bool EnableAnalytics { get; set; } = false;
}
