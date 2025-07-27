using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TaskListMcp.Core.Services;
using TaskListMcp.Models;
using TaskListMcp.Tests.Unit.Helpers;
using Xunit;

namespace TaskListMcp.Tests.Unit.Services;

/// <summary>
/// Unit tests for TaskService
/// </summary>
public class TaskServiceTests : IClassFixture<TestFixture>, IDisposable
{
    private readonly TestFixture _fixture;
    private readonly TaskService _taskService;
    private readonly TestDataBuilder _testDataBuilder;
    private readonly IServiceScope _scope;

    public TaskServiceTests(TestFixture fixture)
    {
        _fixture = fixture;
        _scope = _fixture.CreateScope();
        _taskService = _scope.ServiceProvider.GetRequiredService<TaskService>();
        _testDataBuilder = new TestDataBuilder(_taskService);
    }

    [Fact]
    public async Task CreateTaskAsync_WithValidData_ShouldCreateTask()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var title = "Test Task";
        var description = "Test Description";

        // Act
        var result = await _taskService.CreateTaskAsync(title, description);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be(title);
        result.Description.Should().Be(description);
        result.Status.Should().Be(Models.TaskStatus.Pending);
        result.ListId.Should().Be(1); // Default list
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateTaskAsync_WithEmptyTitle_ShouldThrowArgumentException()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var emptyTitle = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _taskService.CreateTaskAsync(emptyTitle));
    }

    [Fact]
    public async Task CreateTaskAsync_WithNullTitle_ShouldThrowArgumentException()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _taskService.CreateTaskAsync(null!));
    }

    [Fact]
    public async Task CreateTaskAsync_WithWhitespaceTitle_ShouldThrowArgumentException()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var whitespaceTitle = "   ";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _taskService.CreateTaskAsync(whitespaceTitle));
    }

    [Fact]
    public async Task CreateTaskAsync_WithDueDate_ShouldSetDueDate()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var title = "Task with Due Date";
        var dueDate = DateTime.UtcNow.AddDays(7);

        // Act
        var result = await _taskService.CreateTaskAsync(title, dueDate: dueDate);

        // Assert
        result.Should().NotBeNull();
        result.DueDate.Should().BeCloseTo(dueDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateTaskAsync_WithPriority_ShouldSetPriority()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var title = "High Priority Task";
        var priority = (int)Priority.High;

        // Act
        var result = await _taskService.CreateTaskAsync(title, priority: priority);

        // Assert
        result.Should().NotBeNull();
        result.Priority.Should().Be(Priority.High);
    }

    [Fact]
    public async Task CreateTaskAsync_WithEstimatedHours_ShouldSetEstimatedHours()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var title = "Task with Estimated Hours";
        var estimatedHours = 4.5m;

        // Act
        var result = await _taskService.CreateTaskAsync(title, estimatedHours: estimatedHours);

        // Assert
        result.Should().NotBeNull();
        result.EstimatedHours.Should().Be(estimatedHours);
    }

    [Fact]
    public async Task GetTaskByIdAsync_WithExistingId_ShouldReturnTask()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var createdTask = await _testDataBuilder.CreateTestTaskAsync();

        // Act
        var result = await _taskService.GetTaskByIdAsync(createdTask.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdTask.Id);
        result.Title.Should().Be(createdTask.Title);
        result.Description.Should().Be(createdTask.Description);
    }

    [Fact]
    public async Task GetTaskByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var nonExistentId = 999999;

        // Act
        var result = await _taskService.GetTaskByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task StartTaskAsync_WithValidTask_ShouldUpdateStatus()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var task = await _testDataBuilder.CreateTestTaskAsync();
        var beforeUpdate = DateTime.UtcNow.AddSeconds(-1); // Give 1 second buffer

        // Act
        var result = await _taskService.StartTaskAsync(task.Id);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(Models.TaskStatus.InProgress);
        result.UpdatedAt.Should().BeAfter(beforeUpdate);
    }

    [Fact]
    public async Task CompleteTaskAsync_WithValidTask_ShouldUpdateStatus()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var task = await _testDataBuilder.CreateInProgressTaskAsync();
        var beforeUpdate = DateTime.UtcNow.AddSeconds(-1); // Give 1 second buffer

        // Act
        var result = await _taskService.CompleteTaskAsync(task.Id);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(Models.TaskStatus.Completed);
        result.UpdatedAt.Should().BeAfter(beforeUpdate);
    }

    [Fact]
    public async Task DeleteTaskAsync_WithExistingTask_ShouldDeleteTask()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var task = await _testDataBuilder.CreateTestTaskAsync();

        // Act
        var result = await _taskService.DeleteTaskAsync(task.Id);

        // Assert
        result.Should().BeTrue();
        
        var deletedTask = await _taskService.GetTaskByIdAsync(task.Id);
        deletedTask.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTaskAsync_WithNonExistentTask_ShouldReturnFalse()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var nonExistentId = 999999;

        // Act
        var result = await _taskService.DeleteTaskAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ListTasksAsync_WithNoFilters_ShouldReturnAllTasks()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var tasks = await _testDataBuilder.CreateMultipleTestTasksAsync(3);

        // Act
        var result = await _taskService.ListTasksAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(t => tasks.Any(created => created.Id == t.Id));
    }

    [Fact]
    public async Task ListTasksAsync_WithStatusFilter_ShouldReturnFilteredTasks()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        await _testDataBuilder.CreateTestTaskAsync("Pending Task", status: Models.TaskStatus.Pending);
        await _testDataBuilder.CreateTestTaskAsync("Completed Task", status: Models.TaskStatus.Completed);
        await _testDataBuilder.CreateTestTaskAsync("In Progress Task", status: Models.TaskStatus.InProgress);

        // Act
        var result = await _taskService.ListTasksAsync(status: Models.TaskStatus.Pending);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Status.Should().Be(Models.TaskStatus.Pending);
        result.First().Title.Should().Be("Pending Task");
    }

    [Fact]
    public async Task ListTasksAsync_WithListIdFilter_ShouldReturnTasksFromSpecificList()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        await _testDataBuilder.CreateTestTaskAsync("Task in List 1", listId: 1);
        // Note: We'd need to create list 2 first in a real scenario
        // For now, we'll test with the default list

        // Act
        var result = await _taskService.ListTasksAsync(listId: 1);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().ListId.Should().Be(1);
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}
