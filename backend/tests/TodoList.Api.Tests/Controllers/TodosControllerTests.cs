using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TodoList.Application.DTOs;

namespace TodoList.Api.Tests.Controllers;

public class TodosControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TodosControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Helper Methods

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string? email = null, string? password = null)
    {
        var client = _factory.CreateClient();
        email ??= $"testuser-{Guid.NewGuid()}@example.com";
        password ??= "TestPass123!";

        // Register user
        var registerRequest = new RegisterRequest { Email = email, Password = password };
        await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Login
        var loginRequest = new LoginRequest { Email = email, Password = password };
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Attach token
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult!.Token);

        return client;
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task GetTodos_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange - use unauthenticated client
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/todos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTodo_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateTodoRequest { Title = "Test Todo" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/todos", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateTodo_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new UpdateTodoRequest { Title = "Updated", IsCompleted = true };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/todos/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteTodo_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/v1/todos/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region User Isolation Tests

    [Fact]
    public async Task GetTodos_ReturnsOnlyUsersTodos()
    {
        // Arrange - Create two authenticated users
        var userAClient = await CreateAuthenticatedClientAsync();
        var userBClient = await CreateAuthenticatedClientAsync();

        // User A creates a todo
        var userATodo = new CreateTodoRequest { Title = "User A Todo" };
        await userAClient.PostAsJsonAsync("/api/v1/todos", userATodo);

        // User B creates a todo
        var userBTodo = new CreateTodoRequest { Title = "User B Todo" };
        await userBClient.PostAsJsonAsync("/api/v1/todos", userBTodo);

        // Act - User A gets their todos
        var responseA = await userAClient.GetAsync("/api/v1/todos");
        var todosA = await responseA.Content.ReadFromJsonAsync<List<TodoItemDto>>();

        // Act - User B gets their todos
        var responseB = await userBClient.GetAsync("/api/v1/todos");
        var todosB = await responseB.Content.ReadFromJsonAsync<List<TodoItemDto>>();

        // Assert
        todosA.Should().ContainSingle(t => t.Title == "User A Todo");
        todosA.Should().NotContain(t => t.Title == "User B Todo");

        todosB.Should().ContainSingle(t => t.Title == "User B Todo");
        todosB.Should().NotContain(t => t.Title == "User A Todo");
    }

    [Fact]
    public async Task GetTodoById_OfAnotherUser_ReturnsNotFound()
    {
        // Arrange
        var userAClient = await CreateAuthenticatedClientAsync();
        var userBClient = await CreateAuthenticatedClientAsync();

        // User A creates a todo
        var createRequest = new CreateTodoRequest { Title = "User A Private Todo" };
        var createResponse = await userAClient.PostAsJsonAsync("/api/v1/todos", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TodoItemDto>();

        // Act - User B tries to access User A's todo
        var response = await userBClient.GetAsync($"/api/v1/todos/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTodo_OfAnotherUser_ReturnsNotFound()
    {
        // Arrange
        var userAClient = await CreateAuthenticatedClientAsync();
        var userBClient = await CreateAuthenticatedClientAsync();

        // User A creates a todo
        var createRequest = new CreateTodoRequest { Title = "User A Todo" };
        var createResponse = await userAClient.PostAsJsonAsync("/api/v1/todos", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TodoItemDto>();

        // Act - User B tries to update User A's todo
        var updateRequest = new UpdateTodoRequest { Title = "Hacked by User B", IsCompleted = true };
        var response = await userBClient.PutAsJsonAsync($"/api/v1/todos/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify the original todo was not modified
        var verifyResponse = await userAClient.GetAsync($"/api/v1/todos/{created.Id}");
        var verifiedTodo = await verifyResponse.Content.ReadFromJsonAsync<TodoItemDto>();
        verifiedTodo!.Title.Should().Be("User A Todo");
        verifiedTodo.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTodo_OfAnotherUser_ReturnsNotFound()
    {
        // Arrange
        var userAClient = await CreateAuthenticatedClientAsync();
        var userBClient = await CreateAuthenticatedClientAsync();

        // User A creates a todo
        var createRequest = new CreateTodoRequest { Title = "User A Todo to Keep" };
        var createResponse = await userAClient.PostAsJsonAsync("/api/v1/todos", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TodoItemDto>();

        // Act - User B tries to delete User A's todo
        var response = await userBClient.DeleteAsync($"/api/v1/todos/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify the todo still exists for User A
        var verifyResponse = await userAClient.GetAsync($"/api/v1/todos/{created.Id}");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region GET /api/v1/todos Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithEmptyList_WhenNoTodosExist()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/todos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var todos = await response.Content.ReadFromJsonAsync<List<TodoItemDto>>();
        todos.Should().NotBeNull();
        todos.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithTodos_AfterCreatingTodos()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();
        var createRequest = new CreateTodoRequest { Title = "Integration Test Todo" };
        await client.PostAsJsonAsync("/api/v1/todos", createRequest);

        // Act
        var response = await client.GetAsync("/api/v1/todos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var todos = await response.Content.ReadFromJsonAsync<List<TodoItemDto>>();
        todos.Should().NotBeNull();
        todos.Should().Contain(t => t.Title == "Integration Test Todo");
    }

    #endregion

    #region GET /api/v1/todos/{id} Tests

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenTodoDoesNotExist()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync($"/api/v1/todos/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithTodo_WhenTodoExists()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();
        var createRequest = new CreateTodoRequest { Title = "GetById Test" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/todos", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TodoItemDto>();

        // Act
        var response = await client.GetAsync($"/api/v1/todos/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var todo = await response.Content.ReadFromJsonAsync<TodoItemDto>();
        todo.Should().NotBeNull();
        todo!.Title.Should().Be("GetById Test");
    }

    #endregion

    #region POST /api/v1/todos Tests

    [Fact]
    public async Task Create_ReturnsCreatedWithTodo_WithValidRequest()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();
        var request = new CreateTodoRequest { Title = "New Todo Item" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/todos", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var todo = await response.Content.ReadFromJsonAsync<TodoItemDto>();
        todo.Should().NotBeNull();
        todo!.Title.Should().Be("New Todo Item");
        todo.IsCompleted.Should().BeFalse();
        todo.Id.Should().NotBeEmpty();
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WithEmptyTitle()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();
        var request = new CreateTodoRequest { Title = "" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/todos", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WithWhitespaceTitle()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();
        var request = new CreateTodoRequest { Title = "   " };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/todos", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT /api/v1/todos/{id} Tests

    [Fact]
    public async Task Update_ReturnsOkWithUpdatedTodo_WhenTodoExists()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();
        var createRequest = new CreateTodoRequest { Title = "Original Title" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/todos", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TodoItemDto>();

        var updateRequest = new UpdateTodoRequest { Title = "Updated Title", IsCompleted = true };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/todos/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<TodoItemDto>();
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("Updated Title");
        updated.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenTodoDoesNotExist()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();
        var updateRequest = new UpdateTodoRequest { Title = "Updated", IsCompleted = false };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/todos/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WithEmptyTitle()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();
        var createRequest = new CreateTodoRequest { Title = "Original" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/todos", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TodoItemDto>();

        var updateRequest = new UpdateTodoRequest { Title = "", IsCompleted = false };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/todos/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region DELETE /api/v1/todos/{id} Tests

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenTodoExists()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();
        var createRequest = new CreateTodoRequest { Title = "To Be Deleted" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/todos", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TodoItemDto>();

        // Act
        var response = await client.DeleteAsync($"/api/v1/todos/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await client.GetAsync($"/api/v1/todos/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenTodoDoesNotExist()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.DeleteAsync($"/api/v1/todos/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
