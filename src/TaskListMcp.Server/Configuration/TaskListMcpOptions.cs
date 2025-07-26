namespace TaskListMcp.Server.Configuration;

/// <summary>
/// Configuration options for the Task List MCP Server
/// </summary>
public class TaskListMcpOptions
{
    public const string SectionName = "TaskListMcp";

    /// <summary>
    /// Database configuration
    /// </summary>
    public DatabaseOptions Database { get; set; } = new();

    /// <summary>
    /// Performance and resource limits
    /// </summary>
    public PerformanceOptions Performance { get; set; } = new();

    /// <summary>
    /// Feature flags
    /// </summary>
    public FeatureOptions Features { get; set; } = new();

    /// <summary>
    /// Security settings
    /// </summary>
    public SecurityOptions Security { get; set; } = new();
}

public class DatabaseOptions
{
    /// <summary>
    /// Path to the SQLite database file
    /// </summary>
    public string Path { get; set; } = "./data/tasks.db";

    /// <summary>
    /// Query timeout in seconds
    /// </summary>
    public int QueryTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable automatic database maintenance
    /// </summary>
    public bool AutoVacuumEnabled { get; set; } = true;

    /// <summary>
    /// Number of days to retain backup files
    /// </summary>
    public int BackupRetentionDays { get; set; } = 30;
}

public class PerformanceOptions
{
    /// <summary>
    /// Maximum number of results to return from queries
    /// </summary>
    public int MaxQueryResults { get; set; } = 1000;

    /// <summary>
    /// Maximum number of concurrent requests
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 100;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Enable query result caching
    /// </summary>
    public bool EnableQueryCaching { get; set; } = true;

    /// <summary>
    /// Cache expiration time in minutes
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 15;
}

public class FeatureOptions
{
    /// <summary>
    /// Enable health check endpoints
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Enable metrics collection
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Enable Swagger documentation
    /// </summary>
    public bool EnableSwagger { get; set; } = true;

    /// <summary>
    /// Enable request/response logging
    /// </summary>
    public bool EnableRequestLogging { get; set; } = false;

    /// <summary>
    /// Enable performance profiling
    /// </summary>
    public bool EnableProfiling { get; set; } = false;
}

public class SecurityOptions
{
    /// <summary>
    /// Require API key authentication
    /// </summary>
    public bool RequireAuthentication { get; set; } = false;

    /// <summary>
    /// Header name for API key
    /// </summary>
    public string ApiKeyHeader { get; set; } = "X-API-Key";

    /// <summary>
    /// Valid API keys (for development/testing)
    /// </summary>
    public List<string> ApiKeys { get; set; } = new();

    /// <summary>
    /// Allowed CORS origins
    /// </summary>
    public List<string> AllowedOrigins { get; set; } = new() { "*" };

    /// <summary>
    /// Enable request rate limiting
    /// </summary>
    public bool EnableRateLimiting { get; set; } = false;

    /// <summary>
    /// Maximum requests per minute per IP
    /// </summary>
    public int RequestsPerMinute { get; set; } = 100;
}
