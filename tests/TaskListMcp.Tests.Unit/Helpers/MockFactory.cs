using Microsoft.Extensions.Logging;
using Moq;
using TaskListMcp.Data;

namespace TaskListMcp.Tests.Unit.Helpers;

/// <summary>
/// Factory for creating mock objects used in testing
/// </summary>
public static class MockFactory
{
    /// <summary>
    /// Creates a mock logger for the specified type
    /// </summary>
    public static Mock<ILogger<T>> CreateLogger<T>()
    {
        var mock = new Mock<ILogger<T>>();
        
        // Setup default behavior for common logging methods
        mock.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback<LogLevel, EventId, object, Exception?, Delegate>(
                (logLevel, eventId, state, exception, formatter) =>
                {
                    // Can add debug output here if needed during testing
                    // Console.WriteLine($"LOG {logLevel}: {formatter.DynamicInvoke(state, exception)}");
                });

        return mock;
    }

    /// <summary>
    /// Creates a mock database connection factory
    /// </summary>
    public static Mock<IDbConnectionFactory> CreateConnectionFactory(string connectionString = "Data Source=:memory:")
    {
        var mock = new Mock<IDbConnectionFactory>();
        
        mock.Setup(x => x.ConnectionString)
            .Returns(connectionString);
            
        // Note: For actual connection creation, we typically want to use real connections
        // in integration tests and mocked behavior in unit tests
        
        return mock;
    }

    /// <summary>
    /// Creates a mock database manager with common setup
    /// </summary>
    public static Mock<DatabaseManager> CreateDatabaseManager()
    {
        var connectionFactoryMock = CreateConnectionFactory();
        var loggerMock = CreateLogger<DatabaseManager>();
        
        var mock = new Mock<DatabaseManager>(connectionFactoryMock.Object, loggerMock.Object);
        
        // Setup common behavior
        mock.Setup(x => x.InitializeDatabaseAsync())
            .Returns(Task.CompletedTask);
            
        return mock;
    }

    /// <summary>
    /// Creates a logger that writes to console for debugging tests
    /// </summary>
    public static ILogger<T> CreateConsoleLogger<T>()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            
        return loggerFactory.CreateLogger<T>();
    }

    /// <summary>
    /// Creates a logger that captures log messages for verification
    /// </summary>
    public static (Mock<ILogger<T>> Mock, List<string> LogMessages) CreateCapturingLogger<T>()
    {
        var logMessages = new List<string>();
        var mock = new Mock<ILogger<T>>();
        
        mock.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback<LogLevel, EventId, object, Exception?, Delegate>(
                (logLevel, eventId, state, exception, formatter) =>
                {
                    var message = formatter.DynamicInvoke(state, exception)?.ToString() ?? "";
                    logMessages.Add($"[{logLevel}] {message}");
                });

        return (mock, logMessages);
    }

    /// <summary>
    /// Verifies that a logger was called with specific parameters
    /// </summary>
    public static void VerifyLoggerCalled<T>(
        Mock<ILogger<T>> loggerMock, 
        LogLevel logLevel, 
        string messageContains,
        Times? times = null)
    {
        times ??= Times.AtLeastOnce();
        
        loggerMock.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(messageContains)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times.Value);
    }

    /// <summary>
    /// Verifies that a logger was called for an exception
    /// </summary>
    public static void VerifyLoggerCalledWithException<T>(
        Mock<ILogger<T>> loggerMock,
        LogLevel logLevel,
        Type exceptionType,
        Times? times = null)
    {
        times ??= Times.AtLeastOnce();
        
        loggerMock.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.Is<Exception>(ex => ex.GetType() == exceptionType),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times.Value);
    }
}
