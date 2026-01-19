using AutoMapper;
using Microsoft.Extensions.Logging;
using TodoList.Application.DTOs;
using TodoList.Domain.Entities;
using TodoList.Domain.Repositories;

namespace TodoList.Application.Services;

public class TodoService : ITodoService
{
    private readonly ITodoRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<TodoService> _logger;

    public TodoService(ITodoRepository repository, IMapper mapper, ILogger<TodoService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<TodoItemDto>> GetAllAsync(string userId)
    {
        var items = await _repository.GetAllAsync();
        var userItems = items.Where(x => x.UserId == userId);
        var orderedItems = userItems.OrderByDescending(x => x.CreatedAt);
        return _mapper.Map<IEnumerable<TodoItemDto>>(orderedItems);
    }

    public async Task<TodoItemDto?> GetByIdAsync(Guid id, string userId)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null || item.UserId != userId)
        {
            return null;
        }
        return _mapper.Map<TodoItemDto>(item);
    }

    public async Task<TodoItemDto> CreateAsync(CreateTodoRequest request, string userId)
    {
        ValidateTitle(request.Title);

        _logger.LogDebug("Creating todo item for user {UserId} with title: {Title}", userId, request.Title);

        var item = _mapper.Map<TodoItem>(request);
        item.Id = Guid.NewGuid();
        item.UserId = userId;
        item.Title = request.Title.Trim();
        item.CreatedAt = DateTime.UtcNow;

        var created = await _repository.AddAsync(item);

        _logger.LogInformation("Created todo item {TodoId} for user {UserId}", created.Id, userId);
        return _mapper.Map<TodoItemDto>(created);
    }

    public async Task<TodoItemDto?> UpdateAsync(Guid id, UpdateTodoRequest request, string userId)
    {
        ValidateTitle(request.Title);

        var existing = await _repository.GetByIdAsync(id);
        if (existing is null || existing.UserId != userId)
        {
            return null;
        }

        // Updates existing entity
        _mapper.Map(request, existing);
        existing.Title = request.Title.Trim();

        var updated = await _repository.UpdateAsync(existing);
        return updated is null ? null : _mapper.Map<TodoItemDto>(updated);
    }

    public async Task<bool> DeleteAsync(Guid id, string userId)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null || existing.UserId != userId)
        {
            return false;
        }

        return await _repository.DeleteAsync(id);
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty or whitespace only", nameof(title));
        }
    }
}
