using TodoList.Application.DTOs;

namespace TodoList.Application.Services;

public interface ITodoService
{
    Task<IEnumerable<TodoItemDto>> GetAllAsync(string userId);
    Task<TodoItemDto?> GetByIdAsync(Guid id, string userId);
    Task<TodoItemDto> CreateAsync(CreateTodoRequest request, string userId);
    Task<TodoItemDto?> UpdateAsync(Guid id, UpdateTodoRequest request, string userId);
    Task<bool> DeleteAsync(Guid id, string userId);
}
