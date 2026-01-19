using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TodoList.Application.DTOs;
using TodoList.Application.Mappings;
using TodoList.Application.Services;
using TodoList.Domain.Entities;
using TodoList.Domain.Repositories;

namespace TodoList.Application.Tests.Services;

public class TodoServiceTests
{
    private readonly ITodoRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<TodoService> _logger;
    private readonly TodoService _sut;
    private const string TestUserId = "test-user-id";
    private const string OtherUserId = "other-user-id";

    public TodoServiceTests()
    {
        _repository = Substitute.For<ITodoRepository>();
        _logger = NullLogger<TodoService>.Instance;

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TodoMappingProfile>();
        }, NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _sut = new TodoService(_repository, _mapper, _logger);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoTodosExist()
    {
        // Arrange
        _repository.GetAllAsync().Returns(Enumerable.Empty<TodoItem>());

        // Act
        var result = await _sut.GetAllAsync(TestUserId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllTodos_WhenTodosExist()
    {
        // Arrange
        var todos = new List<TodoItem>
        {
            new() { Id = Guid.NewGuid(), UserId = TestUserId, Title = "Todo 1", IsCompleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), UserId = TestUserId, Title = "Todo 2", IsCompleted = true, CreatedAt = DateTime.UtcNow.AddMinutes(-1) }
        };
        _repository.GetAllAsync().Returns(todos);

        // Act
        var result = await _sut.GetAllAsync(TestUserId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTodosOrderedByCreatedAtDescending()
    {
        // Arrange
        var olderTodo = new TodoItem { Id = Guid.NewGuid(), UserId = TestUserId, Title = "Older", CreatedAt = DateTime.UtcNow.AddHours(-1) };
        var newerTodo = new TodoItem { Id = Guid.NewGuid(), UserId = TestUserId, Title = "Newer", CreatedAt = DateTime.UtcNow };
        _repository.GetAllAsync().Returns(new[] { olderTodo, newerTodo });

        // Act
        var result = (await _sut.GetAllAsync(TestUserId)).ToList();

        // Assert
        result[0].Title.Should().Be("Newer");
        result[1].Title.Should().Be("Older");
    }

    [Fact]
    public async Task GetAllAsync_FiltersByUserId()
    {
        // Arrange
        var userTodo1 = new TodoItem { Id = Guid.NewGuid(), UserId = TestUserId, Title = "User Todo 1", CreatedAt = DateTime.UtcNow };
        var userTodo2 = new TodoItem { Id = Guid.NewGuid(), UserId = TestUserId, Title = "User Todo 2", CreatedAt = DateTime.UtcNow.AddMinutes(-1) };
        var otherUserTodo = new TodoItem { Id = Guid.NewGuid(), UserId = OtherUserId, Title = "Other User Todo", CreatedAt = DateTime.UtcNow };
        _repository.GetAllAsync().Returns(new[] { userTodo1, userTodo2, otherUserTodo });

        // Act
        var result = (await _sut.GetAllAsync(TestUserId)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.Title.Should().StartWith("User Todo"));
        result.Should().NotContain(t => t.Title == "Other User Todo");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ReturnsTodo_WhenTodoExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var todo = new TodoItem { Id = id, UserId = TestUserId, Title = "Test Todo", IsCompleted = false, CreatedAt = DateTime.UtcNow };
        _repository.GetByIdAsync(id).Returns(todo);

        // Act
        var result = await _sut.GetByIdAsync(id, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Title.Should().Be("Test Todo");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenTodoDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id).Returns((TodoItem?)null);

        // Act
        var result = await _sut.GetByIdAsync(id, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenUserIdDoesNotMatch()
    {
        // Arrange
        var id = Guid.NewGuid();
        var todo = new TodoItem { Id = id, UserId = OtherUserId, Title = "Other User Todo", IsCompleted = false, CreatedAt = DateTime.UtcNow };
        _repository.GetByIdAsync(id).Returns(todo);

        // Act
        var result = await _sut.GetByIdAsync(id, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_CreatesAndReturnsTodo_WithValidTitle()
    {
        // Arrange
        var request = new CreateTodoRequest { Title = "New Todo" };
        _repository.AddAsync(Arg.Any<TodoItem>()).Returns(callInfo => callInfo.Arg<TodoItem>());

        // Act
        var result = await _sut.CreateAsync(request, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Todo");
        result.IsCompleted.Should().BeFalse();
        result.Id.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(Arg.Is<TodoItem>(t => t.UserId == TestUserId));
    }

    [Fact]
    public async Task CreateAsync_SetsUserId()
    {
        // Arrange
        var request = new CreateTodoRequest { Title = "New Todo" };
        TodoItem? capturedItem = null;
        _repository.AddAsync(Arg.Do<TodoItem>(t => capturedItem = t)).Returns(callInfo => callInfo.Arg<TodoItem>());

        // Act
        await _sut.CreateAsync(request, TestUserId);

        // Assert
        capturedItem.Should().NotBeNull();
        capturedItem!.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task CreateAsync_TrimsTitle()
    {
        // Arrange
        var request = new CreateTodoRequest { Title = "  Trimmed Title  " };
        _repository.AddAsync(Arg.Any<TodoItem>()).Returns(callInfo => callInfo.Arg<TodoItem>());

        // Act
        var result = await _sut.CreateAsync(request, TestUserId);

        // Assert
        result.Title.Should().Be("Trimmed Title");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateAsync_ThrowsArgumentException_WithEmptyOrWhitespaceTitle(string? title)
    {
        // Arrange
        var request = new CreateTodoRequest { Title = title! };

        // Act
        var act = () => _sut.CreateAsync(request, TestUserId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Title cannot be empty or whitespace only*");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_UpdatesAndReturnsTodo_WhenTodoExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new TodoItem { Id = id, UserId = TestUserId, Title = "Old Title", IsCompleted = false, CreatedAt = DateTime.UtcNow };
        var request = new UpdateTodoRequest { Title = "New Title", IsCompleted = true };

        _repository.GetByIdAsync(id).Returns(existing);
        _repository.UpdateAsync(Arg.Any<TodoItem>()).Returns(callInfo => callInfo.Arg<TodoItem>());

        // Act
        var result = await _sut.UpdateAsync(id, request, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("New Title");
        result.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenTodoDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateTodoRequest { Title = "New Title", IsCompleted = false };
        _repository.GetByIdAsync(id).Returns((TodoItem?)null);

        // Act
        var result = await _sut.UpdateAsync(id, request, TestUserId);

        // Assert
        result.Should().BeNull();
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<TodoItem>());
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenUserIdDoesNotMatch()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new TodoItem { Id = id, UserId = OtherUserId, Title = "Other User Todo", IsCompleted = false, CreatedAt = DateTime.UtcNow };
        var request = new UpdateTodoRequest { Title = "New Title", IsCompleted = true };

        _repository.GetByIdAsync(id).Returns(existing);

        // Act
        var result = await _sut.UpdateAsync(id, request, TestUserId);

        // Assert
        result.Should().BeNull();
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<TodoItem>());
    }

    [Fact]
    public async Task UpdateAsync_TrimsTitle()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new TodoItem { Id = id, UserId = TestUserId, Title = "Old", IsCompleted = false, CreatedAt = DateTime.UtcNow };
        var request = new UpdateTodoRequest { Title = "  Trimmed  ", IsCompleted = false };

        _repository.GetByIdAsync(id).Returns(existing);
        _repository.UpdateAsync(Arg.Any<TodoItem>()).Returns(callInfo => callInfo.Arg<TodoItem>());

        // Act
        var result = await _sut.UpdateAsync(id, request, TestUserId);

        // Assert
        result!.Title.Should().Be("Trimmed");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UpdateAsync_ThrowsArgumentException_WithEmptyOrWhitespaceTitle(string? title)
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateTodoRequest { Title = title!, IsCompleted = false };

        // Act
        var act = () => _sut.UpdateAsync(id, request, TestUserId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Title cannot be empty or whitespace only*");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenTodoDeleted()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new TodoItem { Id = id, UserId = TestUserId, Title = "To Delete", IsCompleted = false, CreatedAt = DateTime.UtcNow };
        _repository.GetByIdAsync(id).Returns(existing);
        _repository.DeleteAsync(id).Returns(true);

        // Act
        var result = await _sut.DeleteAsync(id, TestUserId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenTodoDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id).Returns((TodoItem?)null);

        // Act
        var result = await _sut.DeleteAsync(id, TestUserId);

        // Assert
        result.Should().BeFalse();
        await _repository.DidNotReceive().DeleteAsync(id);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenUserIdDoesNotMatch()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new TodoItem { Id = id, UserId = OtherUserId, Title = "Other User Todo", IsCompleted = false, CreatedAt = DateTime.UtcNow };
        _repository.GetByIdAsync(id).Returns(existing);

        // Act
        var result = await _sut.DeleteAsync(id, TestUserId);

        // Assert
        result.Should().BeFalse();
        await _repository.DidNotReceive().DeleteAsync(id);
    }

    #endregion
}
