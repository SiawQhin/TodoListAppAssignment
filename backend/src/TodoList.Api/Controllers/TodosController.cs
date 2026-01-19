using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoList.Application.DTOs;
using TodoList.Application.Services;

namespace TodoList.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TodosController : ControllerBase
{
    private readonly ITodoService _todoService;
    private readonly ILogger<TodosController> _logger;

    public TodosController(ITodoService todoService, ILogger<TodosController> logger)
    {
        _todoService = todoService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TodoItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TodoItemDto>>> GetAll()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Fetching all todos for user: {UserId}", userId);

        var todos = await _todoService.GetAllAsync(userId);

        _logger.LogInformation("Retrieved {Count} todos for user: {UserId}", todos.Count(), userId);
        return Ok(todos);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TodoItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoItemDto>> GetById(Guid id)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Fetching todo {TodoId} for user: {UserId}", id, userId);

        var todo = await _todoService.GetByIdAsync(id, userId);
        if (todo is null)
        {
            _logger.LogWarning("Todo {TodoId} not found for user: {UserId}", id, userId);
            return NotFound();
        }

        return Ok(todo);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TodoItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TodoItemDto>> Create([FromBody] CreateTodoRequest request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Creating new todo for user: {UserId}", userId);

        try
        {
            var todo = await _todoService.CreateAsync(request, userId);
            _logger.LogInformation("Todo {TodoId} created successfully for user: {UserId}", todo.Id, userId);

            return CreatedAtAction(nameof(GetById), new { id = todo.Id }, todo);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Failed to create todo for user {UserId}: {Error}", userId, ex.Message);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = ex.Message
            });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TodoItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoItemDto>> Update(Guid id, [FromBody] UpdateTodoRequest request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Updating todo {TodoId} for user: {UserId}", id, userId);

        try
        {
            var todo = await _todoService.UpdateAsync(id, request, userId);
            if (todo is null)
            {
                _logger.LogWarning("Todo {TodoId} not found for update by user: {UserId}", id, userId);
                return NotFound();
            }

            _logger.LogInformation("Todo {TodoId} updated successfully for user: {UserId}", id, userId);
            return Ok(todo);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Failed to update todo {TodoId} for user {UserId}: {Error}", id, userId, ex.Message);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = ex.Message
            });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Deleting todo {TodoId} for user: {UserId}", id, userId);

        var deleted = await _todoService.DeleteAsync(id, userId);
        if (!deleted)
        {
            _logger.LogWarning("Todo {TodoId} not found for deletion by user: {UserId}", id, userId);
            return NotFound();
        }

        _logger.LogInformation("Todo {TodoId} deleted successfully for user: {UserId}", id, userId);
        return NoContent();
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token");
    }
}
